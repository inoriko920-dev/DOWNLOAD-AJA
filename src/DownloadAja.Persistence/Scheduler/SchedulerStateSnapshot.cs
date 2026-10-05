namespace DownloadAja.Persistence.Scheduler;

/// <summary>
/// RECONSTRUCTED persisted daily queue scheduler contract.
/// Times are stored as local wall-clock minutes from midnight so the schedule
/// remains understandable to the user after restart.
/// </summary>
public sealed record SchedulerStateSnapshot(
    int SchemaVersion,
    bool Enabled,
    int StartMinuteOfDay,
    int StopMinuteOfDay)
{
    public const int CurrentSchemaVersion = 1;

    public static SchedulerStateSnapshot CreateDefault() => new(
        CurrentSchemaVersion,
        Enabled: false,
        StartMinuteOfDay: 8 * 60,
        StopMinuteOfDay: 22 * 60);

    public TimeSpan StartTime => TimeSpan.FromMinutes(StartMinuteOfDay);
    public TimeSpan StopTime => TimeSpan.FromMinutes(StopMinuteOfDay);

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Unsupported scheduler schema version: {SchemaVersion}.");
        }

        if (StartMinuteOfDay is < 0 or > 1439)
        {
            throw new InvalidDataException("Scheduler start minute must be between 0 and 1439.");
        }

        if (StopMinuteOfDay is < 0 or > 1439)
        {
            throw new InvalidDataException("Scheduler stop minute must be between 0 and 1439.");
        }

        if (StartMinuteOfDay == StopMinuteOfDay)
        {
            throw new InvalidDataException("Scheduler start and stop time cannot be identical.");
        }
    }
}
