namespace MortysDBot.Core;

public interface IBotInfo
{
    string Name { get; }
    string Version { get; }
    string EnvironmentName { get; }
    DateTimeOffset StartedAtUtc { get; }
    string Runtime { get; }
    string MachineName { get; }
}
