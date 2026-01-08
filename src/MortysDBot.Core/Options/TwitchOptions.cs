namespace MortysDBot.Core.Options;

public sealed class TwitchOptions
{
    public const string SectionName = "Twitch";

    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";

    public List<TwitchChannelOptions> Channels { get; init; } = new();
}

public sealed class TwitchChannelOptions
{
    /// <summary>Ein freundlicher Name nur für Logs.</summary>
    public string Name { get; init; } = "";

    /// <summary>Twitch broadcaster_id für /helix/schedule.</summary>
    public string BroadcasterId { get; init; } = "";

    /// <summary>URL fürs Discord External Event (Location).</summary>
    public string ChannelUrl { get; init; } = "";

    /// <summary>Discord Guild, in der Events erstellt werden.</summary>
    public ulong TargetGuildId { get; init; }

    /// <summary>Optional: pro Channel abschaltbar.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Optional: pro Channel überschreiben; 0 = Default aus ScheduleSyncOptions verwenden.</summary>
    public int MaxDaysAheadOverride { get; init; } = 0;
}
