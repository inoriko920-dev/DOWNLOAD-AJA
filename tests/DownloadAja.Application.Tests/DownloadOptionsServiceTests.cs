using DownloadAja.Application.Queue;
using DownloadAja.Application.Settings;
using DownloadAja.Infrastructure.Aria2;
using DownloadAja.Persistence.Settings;
using Xunit;

namespace DownloadAja.Application.Tests;

public sealed class DownloadOptionsServiceTests
{
    [Fact]
    public async Task Apply_persists_folder_connections_queue_limit_and_live_speed_cap()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DownloadAja.Options.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var settingsStore = new MemoryAppSettingsStore(AppSettingsSnapshot.CreateDefault());
            var queue = new DownloadQueueCoordinator(
                new FakeDownloadEngine(),
                new MemoryDownloadStore(),
                new MemoryQueueStateStore());
            await queue.InitializeAsync();

            var aria2Options = new Aria2Options("aria2c.exe", 6800, "secret", 8).Validated();
            var rpc = new FakeRpcClient(isHealthy: true);
            var service = new DownloadOptionsService(settingsStore, queue, aria2Options, rpc);

            var appliedLive = await service.ApplyAsync(new DownloadOptionsSnapshot(
                directory,
                MaxConnectionsPerDownload: 12,
                MaxSimultaneousDownloads: 5,
                GlobalDownloadLimitBytesPerSecond: 2 * 1024 * 1024));

            Assert.True(appliedLive);
            Assert.True(Directory.Exists(directory));
            Assert.Equal(Path.GetFullPath(directory), settingsStore.State.DefaultDownloadDirectory);
            Assert.Equal(12, settingsStore.State.MaxConnectionsPerDownload);
            Assert.Equal(2 * 1024 * 1024, settingsStore.State.GlobalDownloadLimitBytesPerSecond);
            Assert.Equal(12, aria2Options.SplitCount);
            Assert.Equal(2 * 1024 * 1024, aria2Options.GlobalDownloadLimitBytesPerSecond);
            Assert.Equal(2 * 1024 * 1024, rpc.ChangedLimit);

            var queueSnapshot = await queue.GetSnapshotAsync();
            Assert.Equal(5, queueSnapshot.MaxSimultaneousDownloads);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Apply_when_aria2_is_not_running_updates_shared_options_without_rpc_call()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DownloadAja.Options.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var settingsStore = new MemoryAppSettingsStore(AppSettingsSnapshot.CreateDefault());
            var queue = new DownloadQueueCoordinator(
                new FakeDownloadEngine(),
                new MemoryDownloadStore(),
                new MemoryQueueStateStore());
            await queue.InitializeAsync();

            var aria2Options = new Aria2Options("aria2c.exe", 6800, "secret", 8).Validated();
            var rpc = new FakeRpcClient(isHealthy: false);
            var service = new DownloadOptionsService(settingsStore, queue, aria2Options, rpc);

            var appliedLive = await service.ApplyAsync(new DownloadOptionsSnapshot(
                directory,
                MaxConnectionsPerDownload: 6,
                MaxSimultaneousDownloads: 2,
                GlobalDownloadLimitBytesPerSecond: 512 * 1024));

            Assert.False(appliedLive);
            Assert.Null(rpc.ChangedLimit);
            Assert.Equal(6, aria2Options.SplitCount);
            Assert.Equal(512 * 1024, aria2Options.GlobalDownloadLimitBytesPerSecond);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class MemoryAppSettingsStore : IAppSettingsStore
    {
        public MemoryAppSettingsStore(AppSettingsSnapshot initial)
        {
            State = initial;
        }

        public AppSettingsSnapshot State { get; private set; }

        public Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(State);

        public Task SaveAsync(AppSettingsSnapshot settings, CancellationToken cancellationToken = default)
        {
            settings.Validate();
            State = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRpcClient : IAria2RpcClient
    {
        private readonly bool _isHealthy;

        public FakeRpcClient(bool isHealthy)
        {
            _isHealthy = isHealthy;
        }

        public long? ChangedLimit { get; private set; }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(_isHealthy);
        public Task<string> AddUriAsync(Uri sourceUri, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task PauseAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResumeAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Aria2Status> TellStatusAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ChangeGlobalDownloadLimitAsync(long bytesPerSecond, CancellationToken cancellationToken = default)
        {
            ChangedLimit = bytesPerSecond;
            return Task.CompletedTask;
        }

        public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
