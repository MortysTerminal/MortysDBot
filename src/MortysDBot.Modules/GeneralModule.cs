using Discord.Interactions;

namespace MortysDBot.Modules;

public sealed class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Antwortet mit pong.")]
    public async Task PingAsync()
    => await RespondAsync("pong 🏓", ephemeral: true);
}
