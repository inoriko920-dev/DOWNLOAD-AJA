using DownloadAja.Application.Downloads;
using DownloadAja.Application.Queue;
using DownloadAja.BrowserBridge;
using Xunit;

namespace DownloadAja.Application.Tests;

public sealed class BrowserHandoffTests
{
    [Fact]
    public void Startup_parser_accepts_add_url_forms()
    {
        var spaced = StartupCommandParser.Parse([
            "--add-url",
            "https://example.com/video.mp4"
        ]);
        var equals = StartupCommandParser.Parse([
            "--add-url=https://example.com/audio.mp3"
        ]);

        Assert.NotNull(spaced);
        Assert.Equal(BrowserHandoffProtocol.AddUrlCommand, spaced.Command);
        Assert.Equal("https://example.com/video.mp4", spaced.Url);
        Assert.True(spaced.StartQueueAfterAdd);

        Assert.NotNull(equals);
        Assert.Equal("https://example.com/audio.mp3", equals.Url);
    }

    [Fact]
    public void Single_instance_guard_marks_only_first_guard_as_primary()
    {
        var mutexName = $"Local\\DownloadAja.Tests.{Guid.NewGuid():N}";

        using var first = SingleInstanceGuard.Acquire(mutexName);
        using var second = SingleInstanceGuard.Acquire(mutexName);

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
    }

    [Fact]
    public async Task Pipe_client_does_not_receive_success_before_handler_finishes()
    {
        var pipeName = $"DownloadAja.Tests.{Guid.NewGuid():N}";
        var server = new NamedPipeBrowserHandoffServer(pipeName);
        var client = new NamedPipeBrowserHandoffClient(pipeName, TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var serverTask = server.RunAsync(
            async (request, cancellationToken) =>
            {
                entered.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken);
                return BrowserHandoffResponse.Accept("persisted", Guid.NewGuid());
            },
            cancellation.Token);

        var clientTask = client.SendAsync(
            BrowserHandoffProtocol.CreateAddUrl("https://example.com/file.bin"),
            cancellation.Token);

        await entered.Task.WaitAsync(cancellation.Token);
        Assert.False(clientTask.IsCompleted);

        release.TrySetResult(true);
        var response = await clientTask;

        Assert.True(response.Accepted);
        Assert.Equal("persisted", response.Message);

        cancellation.Cancel();
        await serverTask;
    }

    [Fact]
    public async Task Browser_service_acknowledges_only_after_item_is_persisted()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "DownloadAja.BrowserHandoff.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var engine = new FakeDownloadEngine();
            var downloads = new MemoryDownloadStore();
            var queueState = new MemoryQueueStateStore();
            var coordinator = new DownloadQueueCoordinator(engine, downloads, queueState);
            await coordinator.InitializeAsync();

            var service = new BrowserDownloadHandoffService(
                new AddDownloadService(coordinator),
                tempDirectory);
            var saveCallsBefore = downloads.SaveCalls;

            var response = await service.HandleAsync(
                BrowserHandoffProtocol.CreateAddUrl(
                    "https://example.com/from-browser.zip",
                    startQueueAfterAdd: false));

            Assert.True(response.Accepted);
            Assert.NotNull(response.DownloadId);
            Assert.True(downloads.SaveCalls > saveCallsBefore);

            var snapshot = await coordinator.GetSnapshotAsync();
            var item = Assert.Single(snapshot.Items);
            Assert.Equal(response.DownloadId!.Value, item.Id);
            Assert.Equal("from-browser.zip", item.FileName);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Browser_service_rejects_unknown_protocol_without_persisting()
    {
        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "DownloadAja.BrowserHandoff.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var downloads = new MemoryDownloadStore();
            var coordinator = new DownloadQueueCoordinator(
                new FakeDownloadEngine(),
                downloads,
                new MemoryQueueStateStore());
            await coordinator.InitializeAsync();

            var service = new BrowserDownloadHandoffService(
                new AddDownloadService(coordinator),
                tempDirectory);
            var saveCallsBefore = downloads.SaveCalls;

            var response = await service.HandleAsync(
                new BrowserHandoffRequest(
                    BrowserHandoffProtocol.CurrentVersion + 1,
                    BrowserHandoffProtocol.AddUrlCommand,
                    "https://example.com/rejected.bin",
                    true));

            Assert.False(response.Accepted);
            Assert.Equal(saveCallsBefore, downloads.SaveCalls);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
