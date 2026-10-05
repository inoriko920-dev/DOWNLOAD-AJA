using DownloadAja.Core.Downloads;
using DownloadAja.Infrastructure.Downloads;
using DownloadAja.Persistence.Downloads;
using DownloadAja.Persistence.Queue;

namespace DownloadAja.Application.Tests;

internal sealed class FakeDownloadEngine : IDownloadEngine
{
    private readonly Dictionary<Guid, DownloadItem> _registered = new();
    private readonly Dictionary<Guid, DownloadState> _nextStates = new();

    public List<Guid> StartedIds { get; } = new();
    public List<Guid> StoppedIds { get; } = new();

    public Task StartAsync(DownloadItem item, CancellationToken cancellationToken = default)
    {
        _registered[item.Id] = item;
        StartedIds.Add(item.Id);
        item.TransitionTo(DownloadState.Downloading);
        return Task.CompletedTask;
    }

    public Task PauseAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var item = _registered[downloadId];
        item.TransitionTo(DownloadState.Paused);
        return Task.CompletedTask;
    }

    public Task ResumeAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var item = _registered[downloadId];
        item.TransitionTo(DownloadState.Downloading);
        return Task.CompletedTask;
    }

    public Task StopAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var item = _registered[downloadId];
        if (item.State is not (DownloadState.Stopped or DownloadState.Completed))
        {
            item.TransitionTo(DownloadState.Stopped);
        }

        _registered.Remove(downloadId);
        StoppedIds.Add(downloadId);
        return Task.CompletedTask;
    }

    public Task<DownloadEngineSnapshot?> GetStatusAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        if (!_registered.TryGetValue(downloadId, out var item))
        {
            return Task.FromResult<DownloadEngineSnapshot?>(null);
        }

        if (_nextStates.Remove(downloadId, out var nextState))
        {
            switch (nextState)
            {
                case DownloadState.Completed when item.State == DownloadState.Downloading:
                    item.UpdateProgress(100, 100, null, null);
                    item.TransitionTo(DownloadState.Completed);
                    _registered.Remove(downloadId);
                    break;
                case DownloadState.Failed when item.State is DownloadState.Downloading or DownloadState.Paused:
                    item.TransitionTo(DownloadState.Failed, "simulated failure");
                    _registered.Remove(downloadId);
                    break;
                case DownloadState.Paused when item.State == DownloadState.Downloading:
                    item.TransitionTo(DownloadState.Paused);
                    break;
            }
        }

        return Task.FromResult<DownloadEngineSnapshot?>(new DownloadEngineSnapshot(
            item.Id,
            item.State,
            item.DownloadedBytes,
            item.TotalBytes,
            item.SpeedBytesPerSecond,
            item.LastError));
    }

    public void CompleteOnNextRefresh(Guid downloadId) => _nextStates[downloadId] = DownloadState.Completed;
    public void FailOnNextRefresh(Guid downloadId) => _nextStates[downloadId] = DownloadState.Failed;
}

internal sealed class MemoryDownloadStore : IDownloadStore
{
    public MemoryDownloadStore(IEnumerable<DownloadItem>? initial = null)
    {
        Items = initial?.ToList() ?? new List<DownloadItem>();
    }

    public List<DownloadItem> Items { get; private set; }
    public int SaveCalls { get; private set; }

    public Task<IReadOnlyList<DownloadItem>> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DownloadItem>>(Items.ToArray());

    public Task SaveAsync(IReadOnlyCollection<DownloadItem> items, CancellationToken cancellationToken = default)
    {
        Items = items.ToList();
        SaveCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class MemoryQueueStateStore : IQueueStateStore
{
    public MemoryQueueStateStore(QueueStateSnapshot? initial = null)
    {
        State = initial ?? QueueStateSnapshot.CreateDefault();
    }

    public QueueStateSnapshot State { get; private set; }
    public int SaveCalls { get; private set; }

    public Task<QueueStateSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);

    public Task SaveAsync(QueueStateSnapshot state, CancellationToken cancellationToken = default)
    {
        state.Validate();
        State = state;
        SaveCalls++;
        return Task.CompletedTask;
    }
}
