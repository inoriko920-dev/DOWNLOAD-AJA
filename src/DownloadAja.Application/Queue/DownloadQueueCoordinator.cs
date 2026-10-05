using DownloadAja.Core.Downloads;
using DownloadAja.Infrastructure.Downloads;
using DownloadAja.Persistence.Downloads;
using DownloadAja.Persistence.Queue;

namespace DownloadAja.Application.Queue;

/// <summary>
/// RECONSTRUCTED deterministic download queue coordinator.
/// Queue ownership is kept outside WPF and outside the aria2 adapter so the
/// historical queue-limit, stop-all, and persisted-order regressions are testable.
/// </summary>
public sealed class DownloadQueueCoordinator
{
    private readonly IDownloadEngine _engine;
    private readonly IDownloadStore _downloadStore;
    private readonly IQueueStateStore _queueStateStore;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<Guid, DownloadItem> _items = new();
    private readonly List<Guid> _order = new();

    private bool _initialized;
    private bool _isRunning;
    private int _maxSimultaneousDownloads = 3;

    public DownloadQueueCoordinator(
        IDownloadEngine engine,
        IDownloadStore downloadStore,
        IQueueStateStore queueStateStore)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _downloadStore = downloadStore ?? throw new ArgumentNullException(nameof(downloadStore));
        _queueStateStore = queueStateStore ?? throw new ArgumentNullException(nameof(queueStateStore));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            var loadedItems = await _downloadStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var queueState = await _queueStateStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            _items.Clear();
            foreach (var item in loadedItems)
            {
                if (!_items.TryAdd(item.Id, item))
                {
                    throw new InvalidDataException($"Duplicate persisted download id: {item.Id}.");
                }

                NormalizeTransientStateAfterRestart(item);
            }

            _order.Clear();
            foreach (var id in queueState.OrderedDownloadIds)
            {
                if (_items.ContainsKey(id) && !_order.Contains(id))
                {
                    _order.Add(id);
                }
            }

            foreach (var item in loadedItems
                         .Where(item => !_order.Contains(item.Id))
                         .OrderBy(item => item.CreatedAt)
                         .ThenBy(item => item.Id))
            {
                _order.Add(item.Id);
            }

            _maxSimultaneousDownloads = queueState.MaxSimultaneousDownloads;
            _isRunning = queueState.IsRunning;
            _initialized = true;

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(DownloadItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();

            if (!_items.TryAdd(item.Id, item))
            {
                throw new InvalidOperationException($"Download {item.Id} already exists in the queue.");
            }

            _order.Add(item.Id);

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StartQueueAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            _isRunning = true;
            await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Stops scheduling new items but leaves currently active downloads running.
    /// Use StopAllAsync for the historical "stop all" command semantics.
    /// </summary>
    public async Task StopQueueAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            _isRunning = false;
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();

            // Critical historical regression guard: turn scheduling off BEFORE
            // stopping active items so freed slots cannot immediately start waits.
            _isRunning = false;

            var activeIds = _order
                .Where(id => _items[id].State is DownloadState.Downloading or DownloadState.Paused)
                .ToArray();

            foreach (var id in activeIds)
            {
                await _engine.StopAsync(id, cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task PauseAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            var item = GetRequiredItem(downloadId);
            if (item.State != DownloadState.Downloading)
            {
                throw new InvalidOperationException($"Cannot pause queue item from state {item.State}.");
            }

            await _engine.PauseAsync(downloadId, cancellationToken).ConfigureAwait(false);

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResumeAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            var item = GetRequiredItem(downloadId);
            if (item.State != DownloadState.Paused)
            {
                throw new InvalidOperationException($"Cannot resume queue item from state {item.State}.");
            }

            if (CountActiveDownloads() >= _maxSimultaneousDownloads)
            {
                throw new InvalidOperationException("No queue slot is available to resume this download.");
            }

            await _engine.ResumeAsync(downloadId, cancellationToken).ConfigureAwait(false);
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            var item = GetRequiredItem(downloadId);

            switch (item.State)
            {
                case DownloadState.Downloading:
                case DownloadState.Paused:
                    await _engine.StopAsync(downloadId, cancellationToken).ConfigureAwait(false);
                    break;

                case DownloadState.Waiting:
                case DownloadState.Failed:
                    item.TransitionTo(DownloadState.Stopped);
                    break;

                case DownloadState.Stopped:
                case DownloadState.Completed:
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported queue item state {item.State}.");
            }

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RestartAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            var item = GetRequiredItem(downloadId);

            if (item.State is not (DownloadState.Stopped or DownloadState.Failed))
            {
                throw new InvalidOperationException($"Cannot requeue download from state {item.State}.");
            }

            item.TransitionTo(DownloadState.Waiting);

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetMaxSimultaneousDownloadsAsync(int value, CancellationToken cancellationToken = default)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Max simultaneous downloads must be at least 1.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            _maxSimultaneousDownloads = value;

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MoveAsync(Guid downloadId, int newIndex, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            _ = GetRequiredItem(downloadId);

            if (newIndex < 0 || newIndex >= _order.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            }

            var oldIndex = _order.IndexOf(downloadId);
            if (oldIndex < 0)
            {
                throw new InvalidDataException($"Download {downloadId} exists but is missing from queue order.");
            }

            if (oldIndex != newIndex)
            {
                _order.RemoveAt(oldIndex);
                _order.Insert(newIndex, downloadId);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();

            var engineOwnedIds = _order
                .Where(id => _items[id].State is DownloadState.Downloading or DownloadState.Paused)
                .ToArray();

            foreach (var id in engineOwnedIds)
            {
                _ = await _engine.GetStatusAsync(id, cancellationToken).ConfigureAwait(false);
            }

            if (_isRunning)
            {
                await FillAvailableSlotsAsync(cancellationToken).ConfigureAwait(false);
            }

            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DownloadQueueSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            return new DownloadQueueSnapshot(
                _order.Select(id => _items[id]).ToArray(),
                _isRunning,
                _maxSimultaneousDownloads,
                CountActiveDownloads());
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task FillAvailableSlotsAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && CountActiveDownloads() < _maxSimultaneousDownloads)
        {
            var next = _order
                .Select(id => _items[id])
                .FirstOrDefault(item => item.State == DownloadState.Waiting);

            if (next is null)
            {
                return;
            }

            await _engine.StartAsync(next, cancellationToken).ConfigureAwait(false);
        }
    }

    private int CountActiveDownloads() => _items.Values.Count(item => item.State == DownloadState.Downloading);

    private DownloadItem GetRequiredItem(Guid downloadId)
    {
        if (downloadId == Guid.Empty)
        {
            throw new ArgumentException("Download id cannot be empty.", nameof(downloadId));
        }

        return _items.TryGetValue(downloadId, out var item)
            ? item
            : throw new KeyNotFoundException($"Download {downloadId} is not present in the queue.");
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var orderedItems = _order.Select(id => _items[id]).ToArray();
        await _downloadStore.SaveAsync(orderedItems, cancellationToken).ConfigureAwait(false);
        await _queueStateStore.SaveAsync(
            new QueueStateSnapshot(
                QueueStateSnapshot.CurrentSchemaVersion,
                _order.ToArray(),
                _maxSimultaneousDownloads,
                _isRunning),
            cancellationToken).ConfigureAwait(false);
    }

    private static void NormalizeTransientStateAfterRestart(DownloadItem item)
    {
        if (item.State == DownloadState.Downloading)
        {
            item.TransitionTo(DownloadState.Stopped);
            item.TransitionTo(DownloadState.Waiting);
            return;
        }

        if (item.State == DownloadState.Paused)
        {
            item.TransitionTo(DownloadState.Stopped);
            item.TransitionTo(DownloadState.Waiting);
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Queue coordinator must be initialized before use.");
        }
    }
}
