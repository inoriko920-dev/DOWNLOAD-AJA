namespace DownloadAja.Application.Downloads;

/// <summary>
/// RECONSTRUCTED request contract for the R7 Add URL flow.
/// FileName is optional; when omitted it is derived from the URL path.
/// </summary>
public sealed record AddDownloadRequest(
    string Url,
    string DestinationDirectory,
    string? FileName,
    bool StartQueueAfterAdd);
