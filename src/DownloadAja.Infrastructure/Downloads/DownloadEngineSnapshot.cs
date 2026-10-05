using DownloadAja.Core.Downloads;

namespace DownloadAja.Infrastructure.Downloads;

public sealed record DownloadEngineSnapshot(
    Guid DownloadId,
    DownloadState State,
    long DownloadedBytes,
    long? TotalBytes,
    double? SpeedBytesPerSecond,
    string? ErrorMessage);
