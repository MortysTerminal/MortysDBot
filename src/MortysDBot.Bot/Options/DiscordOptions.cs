namespace MortysDBot.Bot.Options;

public sealed class DiscordOptions
{
    public const string SectionName = "Discord";

    public string Token { get; init; } = "";
    public ulong GuildId { get; init; }
}
