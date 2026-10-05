using DownloadAja.Persistence.Scheduler;
using Xunit;

namespace DownloadAja.Persistence.Tests;

public sealed class SchedulerStoreTests
{
    [Fact]
    public async Task Scheduler_state_round_trips_through_atomic_json_store()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "DownloadAja.SchedulerStore.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "scheduler.json");
            var store = new JsonSchedulerStateStore(path);
            var expected = new SchedulerStateSnapshot(
                SchedulerStateSnapshot.CurrentSchemaVersion,
                Enabled: true,
                StartMinuteOfDay: 6 * 60 + 30,
                StopMinuteOfDay: 23 * 60 + 15);

            await store.SaveAsync(expected);
            var actual = await store.LoadAsync();

            Assert.Equal(expected, actual);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Missing_scheduler_file_returns_safe_disabled_default()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "DownloadAja.SchedulerStore.Tests",
            Guid.NewGuid().ToString("N"),
            "scheduler.json");

        var store = new JsonSchedulerStateStore(path);
        var state = await store.LoadAsync();

        Assert.False(state.Enabled);
        Assert.Equal(8 * 60, state.StartMinuteOfDay);
        Assert.Equal(22 * 60, state.StopMinuteOfDay);
    }
}
