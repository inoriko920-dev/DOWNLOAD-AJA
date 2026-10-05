using System.Collections.Concurrent;
using DownloadAja.Core.Downloads;
using DownloadAja.Infrastructure.Downloads;

namespace DownloadAja.Infrastructure.Aria2;

/// <summary>
/// RECONSTRUCTED aria2-backed engine adapter.
/// </summary>
public sealed class Aria2DownloadEngine : IDownloadEngine
{
    private readonly IAria2Runtime _runtime;
    private readonly IAria2RpcClient _rpcClient;
    private readonly ConcurrentDictionary<Guid, EngineEntry> _entries = new();

    public Aria2DownloadEngine(IAria2Runtime runtime, IAria2RpcClient rpcClient)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
    }

    public async Task StartAsync(DownloadItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_entries.ContainsKey(item.Id))
        {
            throw new InvalidOperationException($"Download {item.Id} is already registered with aria2.");
        }

        if (item.State is not (DownloadState.Waiting or DownloadState.Stopped or DownloadState.Failed))
        {
            throw new InvalidOperationException($"Cannot start download from state {item.State}.");
        }

        await _runtime.EnsureStartedAsync(cancellationToken).ConfigureAwait(false);
        var gid = await _rpcClient.AddUriAsync(item.SourceUri, item.DestinationPath, cancellationToken).ConfigureAwait(false);

        if (!_entries.TryAdd(item.Id, new EngineEntry(item, gid)))
        {
            throw new InvalidOperationException($"Download {item.Id} was registered concurrently.");
        }

        item.TransitionTo(DownloadState.Downloading);
    }

    public async Task PauseAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var entry = GetRequiredEntry(downloadId);
        if (entry.Item.State != DownloadState.Downloading)
        {
            throw new InvalidOperationException($"Cannot pause download from state {entry.Item.State}.");
        }

        await _rpcClient.PauseAsync(entry.Gid, cancellationToken).ConfigureAwait(false);
        entry.Item.TransitionTo(DownloadState.Paused);
    }

    public async Task ResumeAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var entry = GetRequiredEntry(downloadId);
        if (entry.Item.State != DownloadState.Paused)
        {
            throw new InvalidOperationException($"Cannot resume download from state {entry.Item.State}.");
        }

        await _rpcClient.ResumeAsync(entry.Gid, cancellationToken).ConfigureAwait(false);
        entry.Item.TransitionTo(DownloadState.Downloading);
    }

    public async Task StopAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        var entry = GetRequiredEntry(downloadId);
        if (entry.Item.State == DownloadState.Completed)
        {
            _entries.TryRemove(downloadId, out _);
            return;
        }

        if (entry.Item.State == DownloadState.Stopped)
        {
            _entries.TryRemove(downloadId, out _);
            return;
        }

        await _rpcClient.RemoveAsync(entry.Gid, cancellationToken).ConfigureAwait(false);
        entry.Item.TransitionTo(DownloadState.Stopped);
        _entries.TryRemove(downloadId, out _);
    }

    public async Task<DownloadEngineSnapshot?> GetStatusAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(downloadId, out var entry))
        {
            return null;
        }

        var status = await _rpcClient.TellStatusAsync(entry.Gid, cancellationToken).ConfigureAwait(false);
        var speed = status.DownloadSpeed > 0 ? (double?)status.DownloadSpeed : null;
        var eta = CalculateEta(status.CompletedLength, status.TotalLength, status.DownloadSpeed);
        entry.Item.UpdateProgress(status.CompletedLength, status.TotalLength, speed, eta);

        ApplyAriaState(entry.Item, status);

        var snapshot = new DownloadEngineSnapshot(
            entry.Item.Id,
            entry.Item.State,
            entry.Item.DownloadedBytes,
            entry.Item.TotalBytes,
            entry.Item.SpeedBytesPerSecond,
            entry.Item.LastError);

        if (entry.Item.State is DownloadState.Completed or DownloadState.Failed or DownloadState.Stopped)
        {
            _entries.TryRemove(downloadId, out _);
        }

        return snapshot;
    }

    private EngineEntry GetRequiredEntry(Guid downloadId)
    {
        if (downloadId == Guid.Empty)
        {
            throw new ArgumentException("Download id cannot be empty.", nameof(downloadId));
        }

        return _entries.TryGetValue(downloadId, out var entry)
            ? entry
            : throw new KeyNotFoundException($"Download {downloadId} is not registered with aria2.");
    }

    private static TimeSpan? CalculateEta(long completedLength, long? totalLength, long downloadSpeed)
    {
        if (totalLength is not > 0 || downloadSpeed <= 0 || completedLength >= totalLength.Value)
        {
            return null;
        }

        var remainingBytes = totalLength.Value - completedLength;
        return TimeSpan.FromSeconds((double)remainingBytes / downloadSpeed);
    }

    private static void ApplyAriaState(DownloadItem item, Aria2Status status)
    {
        switch (status.Status)
        {
            case "active":
                if (item.State is DownloadState.Paused or DownloadState.Stopped or DownloadState.Failed)
                {
                    item.TransitionTo(DownloadState.Downloading);
                }
                break;

            case "paused":
                if (item.State == DownloadState.Downloading)
                {
                    item.TransitionTo(DownloadState.Paused);
                }
                break;

            case "complete":
                if (item.State == DownloadState.Downloading)
                {
                    item.TransitionTo(DownloadState.Completed);
                }
                break;

            case "error":
                if (item.State is not (DownloadState.Completed or DownloadState.Stopped or DownloadState.Failed))
                {
                    item.TransitionTo(
                        DownloadState.Failed,
                        string.IsNullOrWhiteSpace(status.ErrorMessage) ? "aria2 reported an unknown download error." : status.ErrorMessage);
                }
                break;

            case "removed":
                if (item.State is not (DownloadState.Completed or DownloadState.Stopped))
                {
                    item.TransitionTo(DownloadState.Stopped);
                }
                break;

            case "waiting":
                break;

            default:
                throw new InvalidDataException($"Unknown aria2 download status: {status.Status}.");
        }
    }

    private sealed record EngineEntry(DownloadItem Item, string Gid);
}
