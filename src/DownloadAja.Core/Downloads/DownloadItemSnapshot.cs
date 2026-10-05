namespace DownloadAja.Core.Downloads;

/// <summary>
/// RECONSTRUCTED versioned serialization contract for persisted download state.
/// Keep backward compatibility explicit when SchemaVersion changes.
/// </summary>
public sealed record DownloadItemSnapshot(
    int SchemaVersion,
    Guid Id,
    string SourceUri,
    string FileName,
    string DestinationPath,
    DownloadCategory Category,
    DownloadState State,
    long DownloadedBytes,
    long? TotalBytes,
    double? SpeedBytesPerSecond,
    TimeSpan? EstimatedTimeRemaining,
    string? LastError,
    DateTimeOffset CreatedAt)
{
    public const int CurrentSchemaVersion = 1;

    public static DownloadItemSnapshot FromItem(DownloadItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new DownloadItemSnapshot(
            CurrentSchemaVersion,
            item.Id,
            item.SourceUri.AbsoluteUri,
            item.FileName,
            item.DestinationPath,
            item.Category,
            item.State,
            item.DownloadedBytes,
            item.TotalBytes,
            item.SpeedBytesPerSecond,
            item.EstimatedTimeRemaining,
            item.LastError,
            item.CreatedAt);
    }

    public DownloadItem ToItem()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Unsupported download snapshot schema version: {SchemaVersion}.");
        }

        if (!Uri.TryCreate(SourceUri, UriKind.Absolute, out var sourceUri))
        {
            throw new InvalidDataException("Persisted download source URI is invalid.");
        }

        return DownloadItem.Restore(
            Id,
            sourceUri,
            FileName,
            DestinationPath,
            Category,
            State,
            DownloadedBytes,
            TotalBytes,
            SpeedBytesPerSecond,
            EstimatedTimeRemaining,
            LastError,
            CreatedAt);
    }
}
