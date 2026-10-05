using System.Diagnostics;

namespace DownloadAja.Infrastructure.Aria2;

/// <summary>
/// RECONSTRUCTED owner for the aria2 child process. It only force-terminates a
/// process instance that it started itself; it never kills an unknown process.
/// </summary>
public sealed class Aria2ProcessManager : IAria2Runtime, IAsyncDisposable
{
    private readonly Aria2Options _options;
    private readonly IAria2RpcClient _rpcClient;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process? _process;

    public Aria2ProcessManager(Aria2Options options, IAria2RpcClient rpcClient)
    {
        _options = options.Validated();
        _rpcClient = rpcClient ?? throw new ArgumentNullException(nameof(rpcClient));
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (await _rpcClient.IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            if (_process is { HasExited: false })
            {
                await WaitForHealthyAsync(_process, cancellationToken).ConfigureAwait(false);
                return;
            }

            ValidateExecutable();
            _process?.Dispose();
            _process = StartProcess();
            await WaitForHealthyAsync(_process, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (await _rpcClient.IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await _rpcClient.ShutdownAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpRequestException or Aria2RpcException or TaskCanceledException)
                {
                    // Fall through to owned-process cleanup. Unknown processes are never force-killed.
                }
            }

            if (_process is null)
            {
                return;
            }

            if (!_process.HasExited)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                try
                {
                    await _process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _process.Kill(entireProcessTree: true);
                    await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            _process.Dispose();
            _process = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Dispose();
            _process?.Dispose();
        }
    }

    private Process StartProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.ArgumentList.Add("--enable-rpc=true");
        startInfo.ArgumentList.Add("--rpc-listen-all=false");
        startInfo.ArgumentList.Add($"--rpc-listen-port={_options.RpcPort}");
        startInfo.ArgumentList.Add($"--rpc-secret={_options.RpcSecret}");
        startInfo.ArgumentList.Add("--rpc-allow-origin-all=false");
        startInfo.ArgumentList.Add("--continue=true");
        startInfo.ArgumentList.Add("--summary-interval=0");
        startInfo.ArgumentList.Add("--console-log-level=warn");

        var executableDirectory = Path.GetDirectoryName(Path.GetFullPath(_options.ExecutablePath));
        if (!string.IsNullOrWhiteSpace(executableDirectory) && Directory.Exists(executableDirectory))
        {
            startInfo.WorkingDirectory = executableDirectory;
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start aria2 process.");
    }

    private async Task WaitForHealthyAsync(Process process, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                throw new InvalidOperationException($"aria2 exited before RPC became ready. Exit code: {process.ExitCode}.");
            }

            if (await _rpcClient.IsHealthyAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken).ConfigureAwait(false);
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("aria2 RPC did not become healthy within 10 seconds.");
    }

    private void ValidateExecutable()
    {
        var pathHasDirectory = Path.IsPathRooted(_options.ExecutablePath)
            || _options.ExecutablePath.Contains(Path.DirectorySeparatorChar)
            || _options.ExecutablePath.Contains(Path.AltDirectorySeparatorChar);

        if (pathHasDirectory && !File.Exists(_options.ExecutablePath))
        {
            throw new FileNotFoundException(
                "aria2c.exe was not found. Place it in tools/aria2/aria2c.exe or set DOWNLOAD_AJA_ARIA2_PATH.",
                _options.ExecutablePath);
        }
    }
}
