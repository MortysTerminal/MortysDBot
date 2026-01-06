using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MortysDBot.Core.Options;

namespace MortysDBot.Modules.Preconditions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireOpsAccessAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<OpsSecurityOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<RequireOpsAccessAttribute>>();

        var userId = context.User.Id;
        var guildId = (context.Guild?.Id);
        var commandName = commandInfo.Name;

        // Owner bypass (wenn gesetzt)
        if (options.BotOwnerId.HasValue && options.BotOwnerId.Value != 0 && userId == options.BotOwnerId.Value)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        // Wenn keine Guild (DM), dann nur Owner
        if (context.Guild is null)
        {
            logger.LogWarning("Ops access denied (DM): User={UserId} Command={Command}", userId, commandName);
            return Task.FromResult(PreconditionResult.FromError("❌ Nicht erlaubt (nur in Servern oder für den Owner)."));
        }

        // Guild Admin Check
        if (options.AllowGuildAdmins && context.User is IGuildUser gu)
        {
            if (gu.GuildPermissions.Administrator)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }

        logger.LogWarning("Ops access denied: User={UserId} Guild={GuildId} Command={Command}", userId, guildId, commandName);
        return Task.FromResult(PreconditionResult.FromError("❌ Keine Berechtigung."));
    }
}
