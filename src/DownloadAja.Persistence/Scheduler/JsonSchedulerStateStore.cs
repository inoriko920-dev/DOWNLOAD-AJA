using DownloadAja.Persistence.Internal;

namespace DownloadAja.Persistence.Scheduler;

/// <summary>
/// RECONSTRUCTED atomic scheduler-state persistence.
/// </summary>
public sealed class JsonSchedulerStateStore : ISchedulerStateStore
{
    private readonly AtomicJsonFile<SchedulerStateSnapshot> _file;

    public JsonSchedulerStateStore(string filePath)
    {
        _file = new AtomicJsonFile<SchedulerStateSnapshot>(filePath);
    }

    public async Task<SchedulerStateSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var state = await _file.LoadAsync(cancellationToken).ConfigureAwait(false)
            ?? SchedulerStateSnapshot.CreateDefault();
        state.Validate();
        return state;
    }

    public Task SaveAsync(SchedulerStateSnapshot state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.Validate();
        return _file.SaveAsync(state, cancellationToken);
    }
}
