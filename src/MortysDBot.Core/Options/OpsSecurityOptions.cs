namespace MortysDBot.Core.Options;

public sealed class OpsSecurityOptions
{
    public const string SectionName = "OpsSecurity";

    public ulong? BotOwnerId { get; init; }
    public bool AllowGuildAdmins { get; init; } = false;
}
