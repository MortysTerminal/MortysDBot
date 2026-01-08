using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MortysDBot.Core.Options;
using MortysDBot.Infrastructure.Twitch;

namespace MortysDBot.Bot.HostedServices;

public sealed class TwitchScheduleDryRunWorker : BackgroundService
{
    private readonly ITwitchScheduleClient _schedule;
    private readonly TwitchOptions _twitch;
    private readonly ScheduleSyncOptions _sync;
    private readonly ILogger<TwitchScheduleDryRunWorker> _log;

    public TwitchScheduleDryRunWorker(
        ITwitchScheduleClient schedule,
        IOptions<TwitchOptions> twitch,
        IOptions<ScheduleSyncOptions> sync,
        ILogger<TwitchScheduleDryRunWorker> log)
    {
        _schedule = schedule;
        _twitch = twitch.Value;
        _sync = sync.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_sync.Enabled)
        {
            _log.LogInformation("Twitch schedule sync disabled (ScheduleSync:Enabled=false).");
            return;
        }

        if (_twitch.Channels.Count == 0)
        {
            _log.LogWarning("No Twitch channels configured (Twitch:Channels is empty).");
            return;
        }

        _log.LogInformation("Twitch schedule dry-run worker started. Interval={Minutes}m DefaultMaxDaysAhead={Days}. Channels={Count}.",
            _sync.IntervalMinutes, _sync.MaxDaysAhead, _twitch.Channels.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var channel in _twitch.Channels.Where(c => c.Enabled))
            {
                using var scope = _log.BeginScope(new Dictionary<string, object>
                {
                    ["TwitchChannel"] = channel.Name,
                    ["BroadcasterId"] = channel.BroadcasterId,
                    ["TargetGuildId"] = channel.TargetGuildId
                });

                try
                {
                    var now = DateTimeOffset.UtcNow;

                    var maxDays = channel.MaxDaysAheadOverride > 0 ? channel.MaxDaysAheadOverride : _sync.MaxDaysAhead;
                    var max = now.AddDays(maxDays);

                    if (string.IsNullOrWhiteSpace(channel.BroadcasterId))
                    {
                        _log.LogWarning("Channel is enabled but BroadcasterId is empty. Skipping.");
                        continue;
                    }

                    var segments = await _schedule.GetSegmentsAsync(channel.BroadcasterId, stoppingToken);

                    var filtered = segments
                        .Where(s => s.StartUtc >= now && s.StartUtc <= max)
                        .OrderBy(s => s.StartUtc)
                        .ToList();

                    _log.LogInformation("DRY-RUN: {Count} segments in next {Days} days.", filtered.Count, maxDays);

                    foreach (var s in filtered.Take(5))
                    {
                        _log.LogInformation("Segment {Id}: {Start} - {End} | {Title} | {Category}",
                            s.Id, s.StartUtc, s.EndUtc, s.Title, s.CategoryName);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Twitch schedule dry-run failed for this channel.");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(_sync.IntervalMinutes), stoppingToken);
        }
    }
}
