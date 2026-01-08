namespace MortysDBot.Core.Sync;

public sealed class ScheduledEventLink
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Source { get; set; } = "twitch";
    public string SourceId { get; set; } = ""; // Twitch segment id

    public ulong GuildId { get; set; }
    public ulong DiscordEventId { get; set; }

    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
