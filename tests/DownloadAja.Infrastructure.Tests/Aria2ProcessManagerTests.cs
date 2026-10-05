using DownloadAja.Infrastructure.Aria2;
using Xunit;

namespace DownloadAja.Infrastructure.Tests;

public sealed class Aria2ProcessManagerTests
{
    [Fact]
    public async Task Missing_bundled_executable_fails_with_actionable_error()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "aria2c.exe");
        var options = new Aria2Options(missingPath, 6800, "secret", 8).Validated();
        var runtime = new Aria2ProcessManager(options, new UnhealthyRpcClient());

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => runtime.EnsureStartedAsync());

        Assert.Contains("DOWNLOAD_AJA_ARIA2_PATH", exception.Message, StringComparison.Ordinal);
        await runtime.DisposeAsync();
    }

    private sealed class UnhealthyRpcClient : IAria2RpcClient
    {
        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<string> AddUriAsync(Uri sourceUri, string destinationPath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task PauseAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ResumeAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Aria2Status> TellStatusAsync(string gid, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
