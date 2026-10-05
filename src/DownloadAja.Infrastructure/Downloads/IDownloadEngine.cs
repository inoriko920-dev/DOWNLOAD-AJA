using DownloadAja.Core.Downloads;

namespace DownloadAja.Infrastructure.Downloads;

/// <summary>
/// RECONSTRUCTED boundary for the external download engine.
/// </summary>
public interface IDownloadEngine
{
    Task StartAsync(DownloadItem item, CancellationToken cancellationToken = default);
    Task PauseAsync(Guid downloadId, CancellationToken cancellationToken = default);
    Task ResumeAsync(Guid downloadId, CancellationToken cancellationToken = default);
    Task StopAsync(Guid downloadId, CancellationToken cancellationToken = default);
    Task<DownloadEngineSnapshot?> GetStatusAsync(Guid downloadId, CancellationToken cancellationToken = default);
}
