using Discord;
using Discord.Interactions;
using MortysDBot.Core;
using MortysDBot.Modules.Preconditions;

namespace MortysDBot.Modules;

[RequireOpsAccess]
public sealed class OpsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IBotInfo _info;

    public OpsModule(IBotInfo info)
        => _info = info;

    [SlashCommand("about", "Zeigt Infos über den Bot (Version, Uptime, Environment).")]
    public async Task AboutAsync()
    {
        var uptime = DateTimeOffset.UtcNow - _info.StartedAtUtc;

        var embed = new EmbedBuilder()
            .WithTitle($"{_info.Name} — About")
            .AddField("Version", _info.Version, inline: true)
            .AddField("Environment", _info.EnvironmentName, inline: true)
            .AddField("Uptime", $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s", inline: true)
            .AddField("Runtime", _info.Runtime, inline: true)
            .AddField("Machine", _info.MachineName, inline: true)
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("health", "Kurzer Gesundheitscheck (später erweiterbar).")]
    public async Task HealthAsync()
    {
        await RespondAsync("✅ OK", ephemeral: true);
    }
}
