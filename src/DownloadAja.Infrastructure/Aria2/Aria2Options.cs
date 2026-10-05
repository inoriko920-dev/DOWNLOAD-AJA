using System.Security.Cryptography;

namespace DownloadAja.Infrastructure.Aria2;

/// <summary>
/// RECONSTRUCTED aria2 runtime options.
/// Historical DOWNLOAD-AJA evidence used up to 20 connections/splits per download.
/// aria2 itself restricts max connections to one server to 16, so the adapter keeps
/// split count up to 20 while clamping per-server connections to 16.
/// Runtime transfer tuning is mutable so the Options dialog can update future
/// downloads without replacing the aria2 adapter.
/// </summary>
public sealed class Aria2Options
{
    public const int MinimumSplitCount = 1;
    public const int MaximumSplitCount = 20;
    public const int MaximumConnectionsPerServer = 16;

    public Aria2Options(
        string executablePath,
        int RpcPort,
        string RpcSecret,
        int SplitCount)
    {
        ExecutablePath = executablePath;
        this.RpcPort = RpcPort;
        this.RpcSecret = RpcSecret;
        this.SplitCount = SplitCount;
    }

    public string ExecutablePath { get; }
    public int RpcPort { get; }
    public string RpcSecret { get; }
    public int SplitCount { get; private set; }
    public long GlobalDownloadLimitBytesPerSecond { get; private set; }

    public int MaxConnectionsPerServer => Math.Min(SplitCount, MaximumConnectionsPerServer);
    public Uri RpcEndpoint => new($"http://127.0.0.1:{RpcPort}/jsonrpc");

    public static Aria2Options CreateDefault(
        string applicationBaseDirectory,
        int splitCount = 8,
        long globalDownloadLimitBytesPerSecond = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationBaseDirectory);

        var configuredPath = Environment.GetEnvironmentVariable("DOWNLOAD_AJA_ARIA2_PATH");
        var executablePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(applicationBaseDirectory, "tools", "aria2", "aria2c.exe")
            : configuredPath;

        var options = new Aria2Options(
            executablePath,
            RpcPort: 6800,
            RpcSecret: Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            SplitCount: splitCount);
        options.UpdateTransferSettings(splitCount, globalDownloadLimitBytesPerSecond);
        return options.Validated();
    }

    public void UpdateTransferSettings(int splitCount, long globalDownloadLimitBytesPerSecond)
    {
        if (splitCount is < MinimumSplitCount or > MaximumSplitCount)
        {
            throw new ArgumentOutOfRangeException(nameof(splitCount), $"Split count must be between {MinimumSplitCount} and {MaximumSplitCount}.");
        }

        if (globalDownloadLimitBytesPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(globalDownloadLimitBytesPerSecond), "Global download limit cannot be negative.");
        }

        SplitCount = splitCount;
        GlobalDownloadLimitBytesPerSecond = globalDownloadLimitBytesPerSecond;
    }

    public Aria2Options Validated()
    {
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            throw new ArgumentException("aria2 executable path cannot be empty.", nameof(ExecutablePath));
        }

        if (RpcPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(RpcPort));
        }

        if (string.IsNullOrWhiteSpace(RpcSecret))
        {
            throw new ArgumentException("aria2 RPC secret cannot be empty.", nameof(RpcSecret));
        }

        if (SplitCount is < MinimumSplitCount or > MaximumSplitCount)
        {
            throw new ArgumentOutOfRangeException(nameof(SplitCount), $"Split count must be between {MinimumSplitCount} and {MaximumSplitCount}.");
        }

        if (GlobalDownloadLimitBytesPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GlobalDownloadLimitBytesPerSecond));
        }

        return this;
    }
}
