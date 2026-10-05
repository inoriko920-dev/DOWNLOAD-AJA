namespace DownloadAja.Infrastructure.Aria2;

public interface IAria2Runtime
{
    Task EnsureStartedAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
