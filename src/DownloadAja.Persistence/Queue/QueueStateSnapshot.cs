namespace DownloadAja.Persistence.Queue;

/// <summary>
/// RECONSTRUCTED persisted queue ordering/settings contract.
/// </summary>
public sealed record QueueStateSnapshot(
    int SchemaVersion,
    Guid[] OrderedDownloadIds,
    int MaxSimultaneousDownloads,
    bool IsRunning)
{
    public const int CurrentSchemaVersion = 1;

    public static QueueStateSnapshot CreateDefault() => new(
        CurrentSchemaVersion,
        Array.Empty<Guid>(),
        MaxSimultaneousDownloads: 3,
        IsRunning: false);

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Unsupported queue state schema version: {SchemaVersion}.");
        }

        if (OrderedDownloadIds is null)
        {
            throw new InvalidDataException("Queue order cannot be null.");
        }

        if (OrderedDownloadIds.Any(static id => id == Guid.Empty))
        {
            throw new InvalidDataException("Queue order contains an empty download id.");
        }

        if (OrderedDownloadIds.Distinct().Count() != OrderedDownloadIds.Length)
        {
            throw new InvalidDataException("Queue order contains duplicate download ids.");
        }

        if (MaxSimultaneousDownloads < 1)
        {
            throw new InvalidDataException("Max simultaneous downloads must be at least 1.");
        }
    }
}
