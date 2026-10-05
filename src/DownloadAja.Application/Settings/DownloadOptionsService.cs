using DownloadAja.Application.Queue;
using DownloadAja.Infrastructure.Aria2;
using DownloadAja.Persistence.Settings;

namespace DownloadAja.Application.Settings;

/// <summary>
/// RECONSTRUCTED options coordinator. App settings own the default folder,
/// per-download split count, and global bandwidth cap; queue state continues to
/// own max simultaneous downloads so there is a single persisted source of truth.
/// </summary>
public sealed class DownloadOptionsService
{
    private readonly IAppSettingsStore _settingsStore;
    private readonly DownloadQueueCoordinator _queue;
    private readonly Aria2Options _aria2Options;
    private readonly IAria2RpcClient _rpcClient;

    public DownloadOptionsService(
        IAppSettingsStore settingsStore,
        DownloadQueueCoordinator queue,
        Aria2Options aria2Options,
        IAria2RpcClient rpcClient)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _aria2Options = aria2Options ?? throw new ArgumentNullException(nameof(aria2Options));
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
    }

    public async Task<DownloadOptionsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        var queue = await _queue.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

        return new DownloadOptionsSnapshot(
            settings.DefaultDownloadDirectory,
            settings.MaxConnectionsPerDownload,
            queue.MaxSimultaneousDownloads,
            settings.GlobalDownloadLimitBytesPerSecond);
    }

    public async Task<bool> ApplyAsync(
        DownloadOptionsSnapshot requested,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requested);
        requested.Validate();

        var normalizedDirectory = Path.GetFullPath(requested.DefaultDownloadDirectory.Trim());
        Directory.CreateDirectory(normalizedDirectory);

        var normalized = requested with { DefaultDownloadDirectory = normalizedDirectory };
        normalized.Validate();

        var settings = new AppSettingsSnapshot(
            AppSettingsSnapshot.CurrentSchemaVersion,
            normalized.DefaultDownloadDirectory,
            normalized.MaxConnectionsPerDownload,
            normalized.GlobalDownloadLimitBytesPerSecond);

        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _queue.SetMaxSimultaneousDownloadsAsync(
            normalized.MaxSimultaneousDownloads,
            cancellationToken).ConfigureAwait(false);

        _aria2Options.UpdateTransferSettings(
            normalized.MaxConnectionsPerDownload,
            normalized.GlobalDownloadLimitBytesPerSecond);

        if (!await _rpcClient.IsHealthyAsync(cancellationToken).ConfigureAwait(false))
        {
            // No aria2 process is active yet. The shared mutable options object will
            // apply the values when Aria2ProcessManager starts it later.
            return false;
        }

        await _rpcClient.ChangeGlobalDownloadLimitAsync(
            normalized.GlobalDownloadLimitBytesPerSecond,
            cancellationToken).ConfigureAwait(false);
        return true;
    }
}
