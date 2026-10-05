using DownloadAja.Application.Queue;
using DownloadAja.Persistence.Scheduler;

namespace DownloadAja.Application.Scheduler;

/// <summary>
/// RECONSTRUCTED daily scheduler for queue start/stop behavior.
/// Scheduler decisions are based on the user's local wall clock and persisted
/// independently from queue state so they survive app restarts.
/// </summary>
public sealed class QueueSchedulerService
{
    private readonly DownloadQueueCoordinator _queue;
    private readonly ISchedulerStateStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SchedulerStateSnapshot _state = SchedulerStateSnapshot.CreateDefault();
    private bool _initialized;

    public QueueSchedulerService(
        DownloadQueueCoordinator queue,
        ISchedulerStateStore store)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task InitializeAsync(
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            _state = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
            await ApplyScheduleCoreAsync(now ?? DateTimeOffset.Now, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SchedulerStateSnapshot> GetStateAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            return _state;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ConfigureAsync(
        bool enabled,
        TimeSpan startTime,
        TimeSpan stopTime,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        ValidateWallClock(startTime, nameof(startTime));
        ValidateWallClock(stopTime, nameof(stopTime));

        var next = new SchedulerStateSnapshot(
            SchedulerStateSnapshot.CurrentSchemaVersion,
            enabled,
            checked((int)startTime.TotalMinutes),
            checked((int)stopTime.TotalMinutes));
        next.Validate();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            _state = next;
            await _store.SaveAsync(_state, cancellationToken).ConfigureAwait(false);
            await ApplyScheduleCoreAsync(now ?? DateTimeOffset.Now, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EvaluateAsync(
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureInitialized();
            await ApplyScheduleCoreAsync(now ?? DateTimeOffset.Now, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ApplyScheduleCoreAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!_state.Enabled)
        {
            return;
        }

        var localNow = now.ToLocalTime().TimeOfDay;
        var shouldRun = IsInsideActiveWindow(
            localNow,
            _state.StartTime,
            _state.StopTime);

        var queueSnapshot = await _queue.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (shouldRun && !queueSnapshot.IsRunning)
        {
            await _queue.StartQueueAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!shouldRun && queueSnapshot.IsRunning)
        {
            // Scheduler stop means stop scheduling new items. Existing active
            // downloads keep running; Stop All remains a separate explicit command.
            await _queue.StopQueueAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public static bool IsInsideActiveWindow(TimeSpan now, TimeSpan start, TimeSpan stop)
    {
        ValidateWallClock(now, nameof(now));
        ValidateWallClock(start, nameof(start));
        ValidateWallClock(stop, nameof(stop));

        if (start == stop)
        {
            throw new ArgumentException("Start and stop time cannot be identical.");
        }

        return start < stop
            ? now >= start && now < stop
            : now >= start || now < stop;
    }

    private static void ValidateWallClock(TimeSpan value, string parameterName)
    {
        if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1) || value.Seconds != 0 || value.Milliseconds != 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Scheduler time must be a minute-aligned local time between 00:00 and 23:59.");
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Scheduler service must be initialized before use.");
        }
    }
}
