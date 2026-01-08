using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MortysDBot.Bot.Services;
using MortysDBot.Core.Options;
using MortysDBot.Core.Sync;
using MortysDBot.Infrastructure.Data;
using MortysDBot.Infrastructure.Twitch;

namespace MortysDBot.Bot.HostedServices;

public sealed class TwitchScheduleSyncWorker : BackgroundService
{
    private readonly ITwitchScheduleClient _schedule;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDiscordScheduledEventService _events;
    private readonly TwitchOptions _twitch;
    private readonly ScheduleSyncOptions _sync;
    private readonly ILogger<TwitchScheduleSyncWorker> _log;

    public TwitchScheduleSyncWorker(
        ITwitchScheduleClient schedule,
        IServiceScopeFactory scopeFactory,
        IDiscordScheduledEventService events,
        IOptions<TwitchOptions> twitch,
        IOptions<ScheduleSyncOptions> sync,
        ILogger<TwitchScheduleSyncWorker> log)
    {
        _schedule = schedule;
        _scopeFactory = scopeFactory;
        _events = events;
        _twitch = twitch.Value;
        _sync = sync.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_sync.Enabled)
        {
            _log.LogInformation("Schedule sync disabled.");
            return;
        }

        _log.LogInformation(
            "Twitch schedule SYNC worker started (CREATE-ONLY). Interval={Minutes}m Channels={Count}.",
            _sync.IntervalMinutes,
            _twitch.Channels.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var channel in _twitch.Channels.Where(c => c.Enabled))
            {
                using var logScope = _log.BeginScope(new Dictionary<string, object>
                {
                    ["TwitchChannel"] = channel.Name,
                    ["BroadcasterId"] = channel.BroadcasterId,
                    ["TargetGuildId"] = channel.TargetGuildId
                });

                try
                {
                    if (string.IsNullOrWhiteSpace(channel.BroadcasterId))
                    {
                        _log.LogWarning("Channel is enabled but BroadcasterId is empty. Skipping.");
                        continue;
                    }

                    // ✅ DbContext scoped erzeugen (pro Channel-Lauf)
                    using var diScope = _scopeFactory.CreateScope();
                    var db = diScope.ServiceProvider.GetRequiredService<MortysDbotDbContext>();

                    var now = DateTimeOffset.UtcNow;
                    var maxDays = channel.MaxDaysAheadOverride > 0 ? channel.MaxDaysAheadOverride : _sync.MaxDaysAhead;
                    var max = now.AddDays(maxDays);

                    var segments = await _schedule.GetSegmentsAsync(channel.BroadcasterId, stoppingToken);
                    var filtered = segments
                        .Where(s => s.StartUtc >= now && s.StartUtc <= max)
                        .OrderBy(s => s.StartUtc)
                        .ToList();

                    _log.LogInformation("Found {Count} segments in next {Days} days.", filtered.Count, maxDays);

                    foreach (var seg in filtered)
                    {
                        var exists = await db.ScheduledEventLinks.AnyAsync(x =>
                            x.Source == "twitch" &&
                            x.SourceId == seg.Id &&
                            x.GuildId == channel.TargetGuildId,
                            stoppingToken);

                        if (exists)
                            continue;

                        var title = seg.Title;
                        var desc = string.IsNullOrWhiteSpace(seg.CategoryName)
                            ? $"Twitch Stream ({channel.Name})"
                            : $"Game: {seg.CategoryName} ({channel.Name})";

                        var locationUrl = string.IsNullOrWhiteSpace(channel.ChannelUrl)
                            ? "https://twitch.tv"
                            : channel.ChannelUrl;

                        var eventId = await _events.CreateExternalEventAsync(
                            guildId: channel.TargetGuildId,
                            name: title,
                            description: desc,
                            locationUrl: locationUrl,
                            startUtc: seg.StartUtc,
                            endUtc: seg.EndUtc,
                            ct: stoppingToken);

                        db.ScheduledEventLinks.Add(new ScheduledEventLink
                        {
                            Source = "twitch",
                            SourceId = seg.Id,
                            GuildId = channel.TargetGuildId,
                            DiscordEventId = eventId,
                            StartUtc = seg.StartUtc,
                            EndUtc = seg.EndUtc,
                            UpdatedAtUtc = DateTimeOffset.UtcNow
                        });

                        await db.SaveChangesAsync(stoppingToken);

                        _log.LogInformation(
                            "Linked Twitch segment {SegmentId} -> Discord event {EventId}.",
                            seg.Id,
                            eventId);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Schedule sync failed for this channel.");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(_sync.IntervalMinutes), stoppingToken);
        }
    }
}
