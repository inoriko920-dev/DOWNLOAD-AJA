using DownloadAja.Core.Downloads;

namespace DownloadAja.Application.Queue;

public sealed record DownloadQueueSnapshot(
    IReadOnlyList<DownloadItem> Items,
    bool IsRunning,
    int MaxSimultaneousDownloads,
    int ActiveDownloads);
