using DownloadAja.Core.Downloads;

namespace DownloadAja.Application.Downloads;

public sealed record AddDownloadResult(
    DownloadItem Item,
    bool QueueStartRequested);
