using DownloadAja.Persistence.Internal;

namespace DownloadAja.Persistence.Queue;

/// <summary>
/// RECONSTRUCTED atomic queue-state persistence.
/// </summary>
public sealed class JsonQueueStateStore : IQueueStateStore
{
    private readonly AtomicJsonFile<QueueStateSnapshot> _file;

    public JsonQueueStateStore(string filePath)
    {
        _file = new AtomicJsonFile<QueueStateSnapshot>(filePath);
    }

    public async Task<QueueStateSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await _file.LoadAsync(cancellationToken).ConfigureAwait(false)
            ?? QueueStateSnapshot.CreateDefault();
        state.Validate();
        return state;
    }

    public Task SaveAsync(QueueStateSnapshot state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Validate();
        return _file.SaveAsync(state, cancellationToken);
    }
}
