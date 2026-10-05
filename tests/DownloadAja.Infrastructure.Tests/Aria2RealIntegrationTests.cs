using System.Net;
using System.Net.Sockets;
using DownloadAja.Core.Downloads;
using DownloadAja.Infrastructure.Aria2;
using Xunit;

namespace DownloadAja.Infrastructure.Tests;

public sealed class Aria2RealIntegrationTests
{
    [Fact]
    [Trait("Category", "Aria2Integration")]
    public async Task Official_aria2_binary_downloads_real_https_file_through_reconstructed_engine()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("DOWNLOAD_AJA_RUN_ARIA2_INTEGRATION"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var executablePath = Environment.GetEnvironmentVariable("DOWNLOAD_AJA_ARIA2_PATH");
        Assert.False(string.IsNullOrWhiteSpace(executablePath));
        Assert.True(File.Exists(executablePath), $"aria2 binary was not found at {executablePath}.");

        var tempDirectory = Path.Combine(Path.GetTempPath(), "DownloadAjaAria2Integration", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var destination = Path.Combine(tempDirectory, "README.rst");

        var options = new Aria2Options(
            executablePath!,
            FindFreeTcpPort(),
            $"integration-{Guid.NewGuid():N}",
            SplitCount: 4).Validated();
        var rpcClient = new Aria2RpcClient(options);
        await using var runtime = new Aria2ProcessManager(options, rpcClient);
        var engine = new Aria2DownloadEngine(runtime, rpcClient);
        var item = new DownloadItem(
            new Uri("https://raw.githubusercontent.com/aria2/aria2/release-1.37.0/README.rst"),
            "README.rst",
            destination);

        try
        {
            await engine.StartAsync(item);

            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTimeOffset.UtcNow < deadline && item.State is not (DownloadState.Completed or DownloadState.Failed))
            {
                await Task.Delay(200);
                _ = await engine.GetStatusAsync(item.Id);
            }

            Assert.Equal(DownloadState.Completed, item.State);
            Assert.True(File.Exists(destination));

            var text = await File.ReadAllTextAsync(destination);
            Assert.Contains("aria2 - The ultra fast download utility", text, StringComparison.Ordinal);
            Assert.True(new FileInfo(destination).Length > 0);
        }
        finally
        {
            await runtime.StopAsync();
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private static int FindFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
