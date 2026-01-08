using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace MortysDBot.Bot.Services;

public interface IDiscordScheduledEventService
{
    Task<ulong> CreateExternalEventAsync(
        ulong guildId,
        string name,
        string description,
        string locationUrl,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct);
}

public sealed class DiscordScheduledEventService : IDiscordScheduledEventService
{
    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordScheduledEventService> _log;

    public DiscordScheduledEventService(
        DiscordSocketClient client,
        ILogger<DiscordScheduledEventService> log)
    {
        _client = client;
        _log = log;
    }

    public async Task<ulong> CreateExternalEventAsync(
        ulong guildId,
        string name,
        string description,
        string locationUrl,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct)
    {
        var guild = _client.GetGuild(guildId)
            ?? throw new InvalidOperationException(
                $"Guild {guildId} not found. Is the bot in the guild?");

        var start = startUtc.UtcDateTime;
        var end = endUtc.UtcDateTime;

        var created = await guild.CreateEventAsync(
            name: name,
            privacyLevel: GuildScheduledEventPrivacyLevel.Private,
            startTime: start,
            endTime: end,
            type: GuildScheduledEventType.External,
            description: description,
            location: locationUrl);

        _log.LogInformation(
            "Created Discord scheduled event {EventId} in guild {GuildId}.",
            created.Id,
            guildId);

        return created.Id;
    }
}
