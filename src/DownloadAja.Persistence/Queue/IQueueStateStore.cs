namespace DownloadAja.Persistence.Queue;

public interface IQueueStateStore
{
    Task<QueueStateSnapshot> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(QueueStateSnapshot state, CancellationToken cancellationToken = default);
}
