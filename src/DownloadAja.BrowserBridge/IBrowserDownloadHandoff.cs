namespace DownloadAja.BrowserBridge;

/// <summary>
/// RECONSTRUCTED application-facing boundary for browser/secondary-instance
/// requests. Success must only be returned after the desktop application has
/// actually accepted and persisted the request.
/// </summary>
public interface IBrowserDownloadHandoff
{
    Task<BrowserHandoffResponse> HandleAsync(
        BrowserHandoffRequest request,
        CancellationToken cancellationToken = default);
}
