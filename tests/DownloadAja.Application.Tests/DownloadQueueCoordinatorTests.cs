using DownloadAja.Application.Queue;
using DownloadAja.Core.Downloads;
using DownloadAja.Persistence.Queue;
using Xunit;

namespace DownloadAja.Application.Tests;

public sealed class DownloadQueueCoordinatorTests
{
    [Fact]
    public async Task Queue_starts_in_persisted_order_and_never_exceeds_limit()
    {
        var a = CreateItem("a.bin");
        var b = CreateItem("b.bin");
        var c = CreateItem("c.bin");
        var engine = new FakeDownloadEngine();
        var downloads = new MemoryDownloadStore(new[] { a, b, c });
        var queueState = new MemoryQueueStateStore(new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            new[] { c.Id, a.Id, b.Id },
            MaxSimultaneousDownloads: 2,
            IsRunning: false));
        var coordinator = new DownloadQueueCoordinator(engine, downloads, queueState);

        await coordinator.InitializeAsync();
        await coordinator.StartQueueAsync();

        Assert.Equal(new[] { c.Id, a.Id }, engine.StartedIds);
        Assert.Equal(DownloadState.Downloading, c.State);
        Assert.Equal(DownloadState.Downloading, a.State);
        Assert.Equal(DownloadState.Waiting, b.State);
        Assert.Equal(2, (await coordinator.GetSnapshotAsync()).ActiveDownloads);
    }

    [Fact]
    public async Task Completing_one_item_starts_exactly_the_next_waiting_item()
    {
        var a = CreateItem("a.bin");
        var b = CreateItem("b.bin");
        var engine = new FakeDownloadEngine();
        var coordinator = CreateCoordinator(engine, new[] { a, b }, max: 1);

        await coordinator.InitializeAsync();
        await coordinator.StartQueueAsync();
        engine.CompleteOnNextRefresh(a.Id);
        await coordinator.RefreshAsync();

        Assert.Equal(DownloadState.Completed, a.State);
        Assert.Equal(DownloadState.Downloading, b.State);
        Assert.Equal(new[] { a.Id, b.Id }, engine.StartedIds);

        var snapshot = await coordinator.GetSnapshotAsync();
        Assert.Contains(snapshot.Items, item => item.Id == a.Id && item.State == DownloadState.Completed);
        Assert.Equal(1, snapshot.ActiveDownloads);
    }

    [Fact]
    public async Task Stop_all_disables_scheduling_before_slots_are_freed()
    {
        var a = CreateItem("a.bin");
        var b = CreateItem("b.bin");
        var engine = new FakeDownloadEngine();
        var coordinator = CreateCoordinator(engine, new[] { a, b }, max: 1);

        await coordinator.InitializeAsync();
        await coordinator.StartQueueAsync();
        await coordinator.StopAllAsync();

        Assert.Equal(new[] { a.Id }, engine.StartedIds);
        Assert.Equal(new[] { a.Id }, engine.StoppedIds);
        Assert.Equal(DownloadState.Stopped, a.State);
        Assert.Equal(DownloadState.Waiting, b.State);

        var snapshot = await coordinator.GetSnapshotAsync();
        Assert.False(snapshot.IsRunning);
        Assert.Equal(0, snapshot.ActiveDownloads);
    }

    [Fact]
    public async Task Pause_frees_slot_but_resume_is_rejected_while_replacement_is_active()
    {
        var a = CreateItem("a.bin");
        var b = CreateItem("b.bin");
        var engine = new FakeDownloadEngine();
        var coordinator = CreateCoordinator(engine, new[] { a, b }, max: 1);

        await coordinator.InitializeAsync();
        await coordinator.StartQueueAsync();
        await coordinator.PauseAsync(a.Id);

        Assert.Equal(DownloadState.Paused, a.State);
        Assert.Equal(DownloadState.Downloading, b.State);
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ResumeAsync(a.Id));
    }

    [Fact]
    public async Task Reorder_is_persisted_and_used_by_new_coordinator_instance()
    {
        var a = CreateItem("a.bin");
        var b = CreateItem("b.bin");
        var c = CreateItem("c.bin");
        var downloadStore = new MemoryDownloadStore(new[] { a, b, c });
        var queueStore = new MemoryQueueStateStore(new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            new[] { a.Id, b.Id, c.Id },
            MaxSimultaneousDownloads: 1,
            IsRunning: false));
        var first = new DownloadQueueCoordinator(new FakeDownloadEngine(), downloadStore, queueStore);

        await first.InitializeAsync();
        await first.MoveAsync(c.Id, 0);

        Assert.Equal(new[] { c.Id, a.Id, b.Id }, queueStore.State.OrderedDownloadIds);

        var secondEngine = new FakeDownloadEngine();
        var second = new DownloadQueueCoordinator(secondEngine, downloadStore, queueStore);
        await second.InitializeAsync();
        await second.StartQueueAsync();

        Assert.Equal(c.Id, Assert.Single(secondEngine.StartedIds));
    }

    [Fact]
    public async Task Persisted_transient_download_is_safely_requeued_after_restart()
    {
        var item = CreateItem("resume.bin");
        item.TransitionTo(DownloadState.Downloading);
        item.UpdateProgress(50, 100, 10, TimeSpan.FromSeconds(5));

        var downloadStore = new MemoryDownloadStore(new[] { item });
        var queueStore = new MemoryQueueStateStore(new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            new[] { item.Id },
            MaxSimultaneousDownloads: 1,
            IsRunning: false));
        var coordinator = new DownloadQueueCoordinator(new FakeDownloadEngine(), downloadStore, queueStore);

        await coordinator.InitializeAsync();

        Assert.Equal(DownloadState.Waiting, item.State);
        Assert.Equal(50, item.DownloadedBytes);
        Assert.Equal(100, item.TotalBytes);
    }

    [Fact]
    public async Task Stopped_item_is_not_automatically_restarted_until_explicitly_requeued()
    {
        var a = CreateItem("a.bin");
        var b = CreateItem("b.bin");
        var engine = new FakeDownloadEngine();
        var coordinator = CreateCoordinator(engine, new[] { a, b }, max: 1);

        await coordinator.InitializeAsync();
        await coordinator.StartQueueAsync();
        await coordinator.StopAsync(a.Id);

        Assert.Equal(DownloadState.Stopped, a.State);
        Assert.Equal(DownloadState.Downloading, b.State);
        Assert.Equal(new[] { a.Id, b.Id }, engine.StartedIds);

        engine.CompleteOnNextRefresh(b.Id);
        await coordinator.RefreshAsync();
        Assert.Equal(DownloadState.Stopped, a.State);

        await coordinator.RestartAsync(a.Id);
        Assert.Equal(DownloadState.Downloading, a.State);
    }

    private static DownloadQueueCoordinator CreateCoordinator(
        FakeDownloadEngine engine,
        IReadOnlyList<DownloadItem> items,
        int max)
    {
        var downloadStore = new MemoryDownloadStore(items);
        var queueStore = new MemoryQueueStateStore(new QueueStateSnapshot(
            QueueStateSnapshot.CurrentSchemaVersion,
            items.Select(item => item.Id).ToArray(),
            max,
            IsRunning: false));
        return new DownloadQueueCoordinator(engine, downloadStore, queueStore);
    }

    private static DownloadItem CreateItem(string fileName) => new(
        new Uri($"https://example.com/{fileName}"),
        fileName,
        Path.Combine(@"C:\Downloads", fileName));
}
