using DownloadAja.Core.Downloads;
using DownloadAja.Infrastructure.Aria2;
using Xunit;

namespace DownloadAja.Infrastructure.Tests;

public sealed class Aria2DownloadEngineTests
{
    [Fact]
    public async Task Start_pause_resume_and_stop_are_mapped_to_rpc_and_domain_state()
    {
        var runtime = new FakeRuntime();
        var rpc = new FakeRpcClient();
        var engine = new Aria2DownloadEngine(runtime, rpc);
        var item = CreateItem();

        await engine.StartAsync(item);
        Assert.Equal(DownloadState.Downloading, item.State);
        Assert.Equal(1, runtime.EnsureStartedCalls);
        Assert.Equal(item.SourceUri, rpc.AddedUri);

        await engine.PauseAsync(item.Id);
        Assert.Equal(DownloadState.Paused, item.State);
        Assert.Equal("gid-1", rpc.PausedGid);

        await engine.ResumeAsync(item.Id);
        Assert.Equal(DownloadState.Downloading, item.State);
        Assert.Equal("gid-1", rpc.ResumedGid);

        await engine.StopAsync(item.Id);
        Assert.Equal(DownloadState.Stopped, item.State);
        Assert.Equal("gid-1", rpc.RemovedGid);
        Assert.Null(await engine.GetStatusAsync(item.Id));
    }

    [Fact]
    public async Task Status_poll_updates_progress_eta_and_completion_state()
    {
        var runtime = new FakeRuntime();
        var rpc = new FakeRpcClient();
        var engine = new Aria2DownloadEngine(runtime, rpc);
        var item = CreateItem();

        await engine.StartAsync(item);
        rpc.Status = new Aria2Status("gid-1", "active", 250, 1000, 125, null);

        var active = await engine.GetStatusAsync(item.Id);

        Assert.NotNull(active);
        Assert.Equal(250, item.DownloadedBytes);
        Assert.Equal(1000, item.TotalBytes);
        Assert.Equal(125d, item.SpeedBytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(6), item.EstimatedTimeRemaining);
        Assert.Equal(DownloadState.Downloading, item.State);

        rpc.Status = new Aria2Status("gid-1", "complete", 1000, 1000, 0, null);
        var completed = await engine.GetStatusAsync(item.Id);

        Assert.NotNull(completed);
        Assert.Equal(DownloadState.Completed, item.State);
        Assert.Null(item.SpeedBytesPerSecond);
        Assert.Null(item.EstimatedTimeRemaining);
    }

    [Fact]
    public async Task Error_status_moves_item_to_failed_and_keeps_message()
    {
        var rpc = new FakeRpcClient();
        var engine = new Aria2DownloadEngine(new FakeRuntime(), rpc);
        var item = CreateItem();
        await engine.StartAsync(item);

        rpc.Status = new Aria2Status("gid-1", "error", 100, 1000, 0, "HTTP 403");
        var snapshot = await engine.GetStatusAsync(item.Id);

        Assert.NotNull(snapshot);
        Assert.Equal(DownloadState.Failed, item.State);
        Assert.Equal("HTTP 403", item.LastError);
    }

    private static DownloadItem CreateItem() => new(
        new Uri("https://example.com/file.iso"),
        "file.iso",
        @"C:\Downloads\file.iso");

    private sealed class FakeRuntime : IAria2Runtime
    {
        public int EnsureStartedCalls { get; private set; }

        public Task EnsureStartedAsync(CancellationToken cancellationToken = default)
        {
            EnsureStartedCalls++;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeRpcClient : IAria2RpcClient
    {
        public Uri? AddedUri { get; private set; }
        public string? PausedGid { get; private set; }
        public string? ResumedGid { get; private set; }
        public string? RemovedGid { get; private set; }
        public Aria2Status Status { get; set; } = new("gid-1", "active", 0, null, 0, null);

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<string> AddUriAsync(Uri sourceUri, string destinationPath, CancellationToken cancellationToken = default)
        {
            AddedUri = sourceUri;
            return Task.FromResult("gid-1");
        }

        public Task PauseAsync(string gid, CancellationToken cancellationToken = default)
        {
            PausedGid = gid;
            return Task.CompletedTask;
        }

        public Task ResumeAsync(string gid, CancellationToken cancellationToken = default)
        {
            ResumedGid = gid;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string gid, CancellationToken cancellationToken = default)
        {
            RemovedGid = gid;
            return Task.CompletedTask;
        }

        public Task<Aria2Status> TellStatusAsync(string gid, CancellationToken cancellationToken = default) => Task.FromResult(Status);

        public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
