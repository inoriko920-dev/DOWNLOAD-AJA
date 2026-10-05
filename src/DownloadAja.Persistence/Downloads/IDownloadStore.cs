using DownloadAja.Core.Downloads;

namespace DownloadAja.Persistence.Downloads;

/// <summary>
/// RECONSTRUCTED persistence boundary. Concrete storage must use atomic writes
/// because historical recovery notes mention save-race defects.
/// </summary>
public interface IDownloadStore
{
    Task<IReadOnlyList<DownloadItem>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyCollection<DownloadItem> items, CancellationToken cancellationToken = default);
}
