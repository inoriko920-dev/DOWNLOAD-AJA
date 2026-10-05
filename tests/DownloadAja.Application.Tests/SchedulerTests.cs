using DownloadAja.Application.Queue;
using DownloadAja.Application.Scheduler;
using DownloadAja.Persistence.Scheduler;
using Xunit;

namespace DownloadAja.Application.Tests;

public sealed class SchedulerTests
{
    [Fact]
    public void Active_window_supports_same_day_and_overnight_ranges()
    {
        Assert.True(QueueSchedulerService.IsInsideActiveWindow(
            TimeSpan.FromHours(10),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(22)));
        Assert.False(QueueSchedulerService.IsInsideActiveWindow(
            TimeSpan.FromHours(23),
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(22)));

        Assert.True(QueueSchedulerService.IsInsideActiveWindow(
            TimeSpan.FromHours(23),
            TimeSpan.FromHours(22),
            TimeSpan.FromHours(6)));
        Assert.True(QueueSchedulerService.IsInsideActiveWindow(
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(22),
            TimeSpan.FromHours(6)));
        Assert.False(QueueSchedulerService.IsInsideActiveWindow(
            TimeSpan.FromHours(12),
            TimeSpan.FromHours(22),
            TimeSpan.FromHours(6)));
    }

    [Fact]
    public async Task Enabled_schedule_starts_queue_inside_window_and_stops_scheduling_outside_window()
    {
        var queue = new DownloadQueueCoordinator(
            new FakeDownloadEngine(),
            new MemoryDownloadStore(),
            new MemoryQueueStateStore());
        await queue.InitializeAsync();

        var store = new MemorySchedulerStateStore(new SchedulerStateSnapshot(
            SchedulerStateSnapshot.CurrentSchemaVersion,
            Enabled: true,
            StartMinuteOfDay: 8 * 60,
            StopMinuteOfDay: 22 * 60));
        var scheduler = new QueueSchedulerService(queue, store);

        await scheduler.InitializeAsync(LocalTime(10, 0));
        Assert.True((await queue.GetSnapshotAsync()).IsRunning);

        await scheduler.EvaluateAsync(LocalTime(23, 0));
        Assert.False((await queue.GetSnapshotAsync()).IsRunning);
    }

    [Fact]
    public async Task Persisted_schedule_is_reapplied_after_service_restart()
    {
        var store = new MemorySchedulerStateStore(SchedulerStateSnapshot.CreateDefault());
        var queue = new DownloadQueueCoordinator(
            new FakeDownloadEngine(),
            new MemoryDownloadStore(),
            new MemoryQueueStateStore());
        await queue.InitializeAsync();

        var first = new QueueSchedulerService(queue, store);
        await first.InitializeAsync(LocalTime(7, 0));
        await first.ConfigureAsync(
            enabled: true,
            startTime: TimeSpan.FromHours(6),
            stopTime: TimeSpan.FromHours(18),
            now: LocalTime(7, 0));
        Assert.True((await queue.GetSnapshotAsync()).IsRunning);

        await queue.StopQueueAsync();
        Assert.False((await queue.GetSnapshotAsync()).IsRunning);

        var restarted = new QueueSchedulerService(queue, store);
        await restarted.InitializeAsync(LocalTime(7, 30));

        Assert.True((await queue.GetSnapshotAsync()).IsRunning);
        Assert.True((await restarted.GetStateAsync()).Enabled);
    }

    [Fact]
    public async Task Disabled_schedule_does_not_override_manual_queue_state()
    {
        var queue = new DownloadQueueCoordinator(
            new FakeDownloadEngine(),
            new MemoryDownloadStore(),
            new MemoryQueueStateStore());
        await queue.InitializeAsync();
        await queue.StartQueueAsync();

        var scheduler = new QueueSchedulerService(
            queue,
            new MemorySchedulerStateStore(SchedulerStateSnapshot.CreateDefault()));
        await scheduler.InitializeAsync(LocalTime(23, 0));

        Assert.True((await queue.GetSnapshotAsync()).IsRunning);
    }

    private static DateTimeOffset LocalTime(int hour, int minute)
    {
        var now = DateTimeOffset.Now;
        return new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            hour,
            minute,
            0,
            now.Offset);
    }

    private sealed class MemorySchedulerStateStore : ISchedulerStateStore
    {
        private SchedulerStateSnapshot _state;

        public MemorySchedulerStateStore(SchedulerStateSnapshot state)
        {
            _state = state;
        }

        public Task<SchedulerStateSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_state);

        public Task SaveAsync(SchedulerStateSnapshot state, CancellationToken cancellationToken = default)
        {
            _state = state;
            return Task.CompletedTask;
        }
    }
}
