namespace DownloadAja.Infrastructure.Aria2;

public interface IAria2RpcClient
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<string> AddUriAsync(Uri sourceUri, string destinationPath, CancellationToken cancellationToken = default);
    Task PauseAsync(string gid, CancellationToken cancellationToken = default);
    Task ResumeAsync(string gid, CancellationToken cancellationToken = default);
    Task RemoveAsync(string gid, CancellationToken cancellationToken = default);
    Task<Aria2Status> TellStatusAsync(string gid, CancellationToken cancellationToken = default);
    Task ChangeGlobalDownloadLimitAsync(long bytesPerSecond, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
