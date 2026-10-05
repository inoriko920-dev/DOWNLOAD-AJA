namespace DownloadAja.Core.Downloads;

/// <summary>
/// RECONSTRUCTED recovery domain entity. This restores the behavior contract first
/// and does not claim to be the original DOWNLOAD-AJA source.
/// </summary>
public sealed class DownloadItem
{
    public DownloadItem(Uri sourceUri, string fileName, string destinationPath)
        : this(
            Guid.NewGuid(),
            sourceUri,
            fileName,
            destinationPath,
            InferCategory(fileName),
            DownloadState.Waiting,
            0,
            null,
            null,
            null,
            null,
            DateTimeOffset.UtcNow)
    {
    }

    private DownloadItem(
        Guid id,
        Uri sourceUri,
        string fileName,
        string destinationPath,
        DownloadCategory category,
        DownloadState state,
        long downloadedBytes,
        long? totalBytes,
        double? speedBytesPerSecond,
        TimeSpan? estimatedTimeRemaining,
        string? lastError,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Download id cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!sourceUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Source URI must be absolute.", nameof(sourceUri));
        }

        ValidateProgress(downloadedBytes, totalBytes, speedBytesPerSecond);

        if (state == DownloadState.Failed && string.IsNullOrWhiteSpace(lastError))
        {
            throw new ArgumentException("Failed state requires an error message.", nameof(lastError));
        }

        Id = id;
        SourceUri = sourceUri;
        FileName = fileName.Trim();
        DestinationPath = destinationPath.Trim();
        Category = category;
        State = state;
        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
        SpeedBytesPerSecond = speedBytesPerSecond;
        EstimatedTimeRemaining = estimatedTimeRemaining;
        LastError = lastError;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Uri SourceUri { get; }
    public string FileName { get; }
    public string DestinationPath { get; }
    public DownloadCategory Category { get; }
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
        ValidateProgress(downloadedBytes, totalBytes, speedBytesPerSecond);

        DownloadedBytes = downloadedBytes;
        TotalBytes = totalBytes;
        SpeedBytesPerSecond = speedBytesPerSecond;
        EstimatedTimeRemaining = eta;
    }

    public static DownloadItem Restore(
        Guid id,
        Uri sourceUri,
        string fileName,
        string destinationPath,
        DownloadCategory category,
        DownloadState state,
        long downloadedBytes,
        long? totalBytes,
        double? speedBytesPerSecond,
        TimeSpan? estimatedTimeRemaining,
        string? lastError,
        DateTimeOffset createdAt) => new(
            id,
            sourceUri,
            fileName,
            destinationPath,
            category,
            state,
            downloadedBytes,
            totalBytes,
            speedBytesPerSecond,
            estimatedTimeRemaining,
            lastError,
            createdAt);

    public static DownloadCategory InferCategory(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" or ".m4v" => DownloadCategory.Video,
            ".mp3" or ".wav" or ".flac" or ".aac" or ".m4a" or ".ogg" or ".opus" => DownloadCategory.Audio,
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".rtf" => DownloadCategory.Document,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz" => DownloadCategory.Archive,
            ".exe" or ".msi" or ".msix" or ".appx" or ".appxbundle" => DownloadCategory.Program,
            _ => DownloadCategory.Other
        };
    }

    private static void ValidateProgress(long downloadedBytes, long? totalBytes, double? speedBytesPerSecond)
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
