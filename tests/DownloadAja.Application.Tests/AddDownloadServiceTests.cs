using DownloadAja.Application.Downloads;
using DownloadAja.Application.Queue;
using DownloadAja.Core.Downloads;
using Xunit;

namespace DownloadAja.Application.Tests;

public sealed class AddDownloadServiceTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"download-aja-add-{Guid.NewGuid():N}");

    [Fact]
    public async Task Add_derives_file_name_and_persists_waiting_item()
    {
        var engine = new FakeDownloadEngine();
        var store = new MemoryDownloadStore();
        var queue = new DownloadQueueCoordinator(engine, store, new MemoryQueueStateStore());
        await queue.InitializeAsync();
        var service = new AddDownloadService(queue);

        var result = await service.AddAsync(new AddDownloadRequest(
            "https://example.com/files/video%20final.mp4?token=abc",
            _tempDirectory,
            null,
            StartQueueAfterAdd: false));

        Assert.Equal("video final.mp4", result.Item.FileName);
        Assert.Equal(DownloadCategory.Video, result.Item.Category);
        Assert.Equal(DownloadState.Waiting, result.Item.State);
        Assert.Single(store.Items);
        Assert.Empty(engine.StartedIds);
        Assert.True(Directory.Exists(_tempDirectory));
    }

    [Fact]
    public async Task Add_with_start_requested_starts_real_queue_item()
    {
        var engine = new FakeDownloadEngine();
        var queue = new DownloadQueueCoordinator(engine, new MemoryDownloadStore(), new MemoryQueueStateStore());
        await queue.InitializeAsync();
        var service = new AddDownloadService(queue);

        var result = await service.AddAsync(new AddDownloadRequest(
            "https://example.com/file.zip",
            _tempDirectory,
            null,
            StartQueueAfterAdd: true));

        Assert.True(result.QueueStartRequested);
        Assert.Equal(DownloadState.Downloading, result.Item.State);
        Assert.Contains(result.Item.Id, engine.StartedIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file.zip")]
    [InlineData("file:///C:/temp/file.zip")]
    public async Task Add_rejects_unsupported_or_invalid_urls(string url)
    {
        var queue = new DownloadQueueCoordinator(new FakeDownloadEngine(), new MemoryDownloadStore(), new MemoryQueueStateStore());
        await queue.InitializeAsync();
        var service = new AddDownloadService(queue);

        await Assert.ThrowsAsync<AddDownloadValidationException>(() => service.AddAsync(new AddDownloadRequest(
            url,
            _tempDirectory,
            null,
            StartQueueAfterAdd: false)));
    }

    [Fact]
    public async Task Add_rejects_duplicate_url_even_with_different_file_name()
    {
        var existing = new DownloadItem(
            new Uri("https://example.com/file.iso"),
            "old.iso",
            Path.Combine(_tempDirectory, "old.iso"));
        var store = new MemoryDownloadStore([existing]);
        var queue = new DownloadQueueCoordinator(new FakeDownloadEngine(), store, new MemoryQueueStateStore());
        await queue.InitializeAsync();
        var service = new AddDownloadService(queue);

        var exception = await Assert.ThrowsAsync<AddDownloadValidationException>(() => service.AddAsync(new AddDownloadRequest(
            "https://example.com/file.iso",
            _tempDirectory,
            "new.iso",
            StartQueueAfterAdd: false)));

        Assert.Contains("URL ini sudah ada", exception.Message);
    }

    [Fact]
    public async Task Add_rejects_duplicate_destination_path()
    {
        var existing = new DownloadItem(
            new Uri("https://example.com/a.bin"),
            "same.bin",
            Path.Combine(_tempDirectory, "same.bin"));
        var store = new MemoryDownloadStore([existing]);
        var queue = new DownloadQueueCoordinator(new FakeDownloadEngine(), store, new MemoryQueueStateStore());
        await queue.InitializeAsync();
        var service = new AddDownloadService(queue);

        var exception = await Assert.ThrowsAsync<AddDownloadValidationException>(() => service.AddAsync(new AddDownloadRequest(
            "https://example.com/b.bin",
            _tempDirectory,
            "same.bin",
            StartQueueAfterAdd: false)));

        Assert.Contains("file tujuan yang sama", exception.Message);
    }

    [Fact]
    public async Task Add_rejects_existing_file_on_disk()
    {
        Directory.CreateDirectory(_tempDirectory);
        var targetPath = Path.Combine(_tempDirectory, "exists.bin");
        await File.WriteAllTextAsync(targetPath, "existing");

        var queue = new DownloadQueueCoordinator(new FakeDownloadEngine(), new MemoryDownloadStore(), new MemoryQueueStateStore());
        await queue.InitializeAsync();
        var service = new AddDownloadService(queue);

        await Assert.ThrowsAsync<AddDownloadValidationException>(() => service.AddAsync(new AddDownloadRequest(
            "https://example.com/exists.bin",
            _tempDirectory,
            null,
            StartQueueAfterAdd: false)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
