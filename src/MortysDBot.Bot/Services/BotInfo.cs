using Microsoft.Extensions.Hosting;
using MortysDBot.Core;

namespace MortysDBot.Bot.Services;

public sealed class BotInfo : IBotInfo
{
    public BotInfo(IHostEnvironment env)
    {
        EnvironmentName = env.EnvironmentName;
        StartedAtUtc = DateTimeOffset.UtcNow;

        Name = "MortysDBot";
        Version = typeof(BotInfo).Assembly.GetName().Version?.ToString() ?? "unknown";
        Runtime = $".NET {Environment.Version}";
        MachineName = Environment.MachineName;
    }

    public string Name { get; }
    public string Version { get; }
    public string EnvironmentName { get; }
    public DateTimeOffset StartedAtUtc { get; }
    public string Runtime { get; }
    public string MachineName { get; }
}
