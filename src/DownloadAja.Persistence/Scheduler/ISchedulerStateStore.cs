namespace DownloadAja.Persistence.Scheduler;

public interface ISchedulerStateStore
{
    Task<SchedulerStateSnapshot> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SchedulerStateSnapshot state, CancellationToken cancellationToken = default);
}
