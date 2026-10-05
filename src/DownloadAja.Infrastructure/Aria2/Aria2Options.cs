using System.Security.Cryptography;

namespace DownloadAja.Infrastructure.Aria2;

/// <summary>
/// RECONSTRUCTED aria2 runtime options.
/// Historical DOWNLOAD-AJA evidence used up to 20 connections/splits per download.
/// aria2 itself restricts max connections to one server to 16, so the adapter keeps
/// split count up to 20 while clamping per-server connections to 16.
/// </summary>
public sealed record Aria2Options(
    string ExecutablePath,
    int RpcPort,
    string RpcSecret,
    int SplitCount)
{
    public const int MinimumSplitCount = 1;
    public const int MaximumSplitCount = 20;
    public const int MaximumConnectionsPerServer = 16;

    public int MaxConnectionsPerServer => Math.Min(SplitCount, MaximumConnectionsPerServer);

    public Uri RpcEndpoint => new($"http://127.0.0.1:{RpcPort}/jsonrpc");

    public static Aria2Options CreateDefault(string applicationBaseDirectory, int splitCount = 8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationBaseDirectory);

        var configuredPath = Environment.GetEnvironmentVariable("DOWNLOAD_AJA_ARIA2_PATH");
        var executablePath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(applicationBaseDirectory, "tools", "aria2", "aria2c.exe")
            : configuredPath;

        return new Aria2Options(
            executablePath,
            RpcPort: 6800,
            RpcSecret: Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
            SplitCount: splitCount).Validated();
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

        return this;
    }
}
