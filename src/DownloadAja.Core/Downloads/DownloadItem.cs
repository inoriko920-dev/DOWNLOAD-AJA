namespace DownloadAja.Core.Downloads;

/// <summary>
/// RECONSTRUCTED recovery domain entity. This is intentionally small: it restores
/// the behavior contract first and does not claim to be the original source.
/// </summary>
public sealed class DownloadItem
{
    public DownloadItem(Uri sourceUri, string fileName, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!sourceUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Source URI must be absolute.", nameof(sourceUri));
        }

        Id = Guid.NewGuid();
        SourceUri = sourceUri;
        FileName = fileName.Trim();
        DestinationPath = destinationPath.Trim();
        State = DownloadState.Waiting;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public Uri SourceUri { get; }
    public string FileName { get; }
    public string DestinationPath { get; }
    public DownloadState State { get; private set; }
    public long DownloadedBytes { get; private set; }
    public long? TotalBytes { get; private set; }
    public double? SpeedBytesPerSecond { get; private set; }
    public TimeSpan? EstimatedTimeRemaining { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public double? ProgressPercent => TotalBytes is > 0
        ? Math.Clamp((double)DownloadedBytes / TotalBytes.Value * 100d, 0d, 100d)
        : null;

    public void TransitionTo(DownloadState nextState, string? error = null)
    {
        if (!CanTransition(State, nextState))
        {
            throw new InvalidOperationException($"Invalid download transition: {State} -> {nextState}.");
        }

        if (nextState == DownloadState.Failed && string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Failed state requires an error message.", nameof(error));
        }

        State = nextState;
        LastError = nextState == DownloadState.Failed ? error : null;

        if (nextState is DownloadState.Completed or DownloadState.Stopped or DownloadState.Failed)
        {
            SpeedBytesPerSecond = null;
            EstimatedTimeRemaining = null;
        }
    }

    public void UpdateProgress(long downloadedBytes, long? totalBytes, double? speedBytesPerSecond, TimeSpan? eta)
    {
        if (downloadedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(downloadedBytes));
        }

        if (totalBytes is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalBytes));
        }

        if (totalBytes.HasValue && downloadedBytes > totalBytes.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(downloadedBytes), "Downloaded bytes cannot exceed total bytes.");
        }

        if (speedBytesPerSecond is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speedBytesPerSecond));
        }

        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
        SpeedBytesPerSecond = speedBytesPerSecond;
        EstimatedTimeRemaining = eta;
    }

    private static bool CanTransition(DownloadState current, DownloadState next) => current switch
    {
        DownloadState.Waiting => next is DownloadState.Downloading or DownloadState.Stopped or DownloadState.Failed,
        DownloadState.Downloading => next is DownloadState.Paused or DownloadState.Completed or DownloadState.Stopped or DownloadState.Failed,
        DownloadState.Paused => next is DownloadState.Downloading or DownloadState.Stopped or DownloadState.Failed,
        DownloadState.Failed => next is DownloadState.Waiting or DownloadState.Downloading or DownloadState.Stopped,
        DownloadState.Completed => false,
        DownloadState.Stopped => next is DownloadState.Waiting or DownloadState.Downloading,
        _ => false
    };
}
