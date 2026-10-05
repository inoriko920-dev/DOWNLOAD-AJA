using DownloadAja.Persistence.Queue;
using DownloadAja.Persistence.Settings;
using Xunit;

namespace DownloadAja.Persistence.Tests;

public sealed class QueueAndSettingsStoreTests
{
    [Fact]
    public async Task Queue_state_round_trip_preserves_order_and_limits()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "queue.json");
        var store = new JsonQueueStateStore(path);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var state = new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            new[] { first, second },
            MaxSimultaneousDownloads: 4,
            IsRunning: true);

        await store.SaveAsync(state);
        var loaded = await store.LoadAsync();

        Assert.Equal(new[] { first, second }, loaded.OrderedDownloadIds);
        Assert.Equal(4, loaded.MaxSimultaneousDownloads);
        Assert.True(loaded.IsRunning);
    }

    [Fact]
    public async Task Missing_queue_state_returns_safe_default()
    {
        using var temp = new TempDirectory();
        var store = new JsonQueueStateStore(Path.Combine(temp.Path, "missing-queue.json"));

        var loaded = await store.LoadAsync();

        Assert.Empty(loaded.OrderedDownloadIds);
        Assert.False(loaded.IsRunning);
        Assert.True(loaded.MaxSimultaneousDownloads >= 1);
    }

    [Fact]
    public void Queue_state_rejects_duplicate_download_ids()
    {
        var id = Guid.NewGuid();
        var state = new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            new[] { id, id },
            MaxSimultaneousDownloads: 2,
            IsRunning: false);

        Assert.Throws<InvalidDataException>(state.Validate);
    }

    [Fact]
    public async Task App_settings_round_trip_preserves_download_path_and_connection_limit()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new JsonAppSettingsStore(path);
        var settings = new AppSettingsSnapshot(
            AppSettingsSnapshot.CurrentSchemaVersion,
            @"D:\Unduhan",
            MaxConnectionsPerDownload: 12);

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(@"D:\Unduhan", loaded.DefaultDownloadDirectory);
        Assert.Equal(12, loaded.MaxConnectionsPerDownload);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(100)]
    public void App_settings_reject_connection_count_outside_historical_aria2_limit(int connectionCount)
    {
        var settings = new AppSettingsSnapshot(
            AppSettingsSnapshot.CurrentSchemaVersion,
            @"C:\Downloads",
            connectionCount);

        Assert.Throws<InvalidDataException>(settings.Validate);
    }

    [Fact]
    public async Task Queue_store_recovers_valid_temp_when_primary_is_corrupt()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "queue.json");
        var store = new JsonQueueStateStore(path);
        var id = Guid.NewGuid();
        var state = new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            new[] { id },
            MaxSimultaneousDownloads: 2,
            IsRunning: false);

        await store.SaveAsync(state);
        File.Move(path, path + ".tmp");
        await File.WriteAllTextAsync(path, "{ broken-json");

        var loaded = await store.LoadAsync();

        Assert.Equal(id, Assert.Single(loaded.OrderedDownloadIds));
        Assert.False(File.Exists(path + ".tmp"));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DownloadAjaTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
