namespace MortysDBot.Core.Options;

public sealed class ScheduleSyncOptions
{
    public const string SectionName = "ScheduleSync";

    public bool Enabled { get; init; } = true;
    public int IntervalMinutes { get; init; } = 30;
    public int MaxDaysAhead { get; init; } = 14;
    public int DeleteHoursAfterEnd { get; init; } = 6;
}
