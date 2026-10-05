namespace DownloadAja.BrowserBridge;

/// <summary>
/// RECONSTRUCTED protocol boundary for browser-to-desktop handoff.
/// The concrete implementation must acknowledge only after the desktop app
/// has accepted the URL, matching a historical regression target.
/// </summary>
public interface IBrowserDownloadHandoff
{
    Task<bool> TryAcceptAsync(Uri sourceUri, CancellationToken cancellationToken = default);
}
