using DownloadAja.Core.Downloads;
using DownloadAja.Persistence.Downloads;
using Xunit;

namespace DownloadAja.Persistence.Tests;

public sealed class JsonDownloadStoreTests
{
    [Fact]
    public async Task Save_and_load_round_trip_preserves_items()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "downloads.json");
        var store = new JsonDownloadStore(path);

        var item = CreateItem("movie.mkv");
        item.TransitionTo(DownloadState.Downloading);
        item.UpdateProgress(250, 1000, 4096, TimeSpan.FromSeconds(3));
        item.TransitionTo(DownloadState.Paused);

        await store.SaveAsync(new[] { item });
        var loaded = await store.LoadAsync();

        var restored = Assert.Single(loaded);
        Assert.Equal(item.Id, restored.Id);
        Assert.Equal(item.FileName, restored.FileName);
        Assert.Equal(item.Category, restored.Category);
        Assert.Equal(item.State, restored.State);
        Assert.Equal(item.DownloadedBytes, restored.DownloadedBytes);
        Assert.Equal(item.TotalBytes, restored.TotalBytes);
        Assert.Equal(item.SpeedBytesPerSecond, restored.SpeedBytesPerSecond);
        Assert.Equal(item.EstimatedTimeRemaining, restored.EstimatedTimeRemaining);
    }

    [Fact]
    public async Task Missing_store_returns_empty_collection()
    {
        using var temp = new TempDirectory();
        var store = new JsonDownloadStore(Path.Combine(temp.Path, "missing.json"));

        var loaded = await store.LoadAsync();

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task Corrupt_primary_recovers_from_valid_temp_file()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "downloads.json");
        var store = new JsonDownloadStore(path);
        var item = CreateItem("report.pdf");

        await store.SaveAsync(new[] { item });
        File.Move(path, path + ".tmp");
        await File.WriteAllTextAsync(path, "{ corrupt-json");

        var loaded = await store.LoadAsync();

        Assert.Equal(item.Id, Assert.Single(loaded).Id);
        Assert.False(File.Exists(path + ".tmp"));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public async Task Concurrent_store_instances_do_not_produce_partial_json()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "downloads.json");
        var storeA = new JsonDownloadStore(path);
        var storeB = new JsonDownloadStore(path);

        var itemA = CreateItem("a.zip");
        var itemB = CreateItem("b.exe");

        await Task.WhenAll(
            storeA.SaveAsync(new[] { itemA }),
            storeB.SaveAsync(new[] { itemB }));

        var loaded = await new JsonDownloadStore(path).LoadAsync();
        var restored = Assert.Single(loaded);

        Assert.Contains(restored.Id, new[] { itemA.Id, itemB.Id });
    }

    private static DownloadItem CreateItem(string fileName) => new(
        new Uri($"https://example.com/{fileName}"),
        fileName,
        Path.Combine(@"C:\Downloads", fileName));

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
