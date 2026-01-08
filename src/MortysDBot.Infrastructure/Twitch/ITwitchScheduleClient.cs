namespace MortysDBot.Infrastructure.Twitch;

public interface ITwitchScheduleClient
{
    Task<IReadOnlyList<TwitchScheduleSegment>> GetSegmentsAsync(string broadcasterId, CancellationToken ct);
}

public sealed record TwitchScheduleSegment(
    string Id,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Title,
    string CategoryName);
