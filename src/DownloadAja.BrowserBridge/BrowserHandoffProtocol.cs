using System.Security.Cryptography;
using System.Text;

namespace DownloadAja.BrowserBridge;

/// <summary>
/// RECONSTRUCTED v1 contract for local browser/secondary-instance handoff.
/// The protocol is deliberately tiny and versioned so the Chrome extension can
/// be added later without coupling transport code to WPF.
/// </summary>
public static class BrowserHandoffProtocol
{
    public const int CurrentVersion = 1;
    public const string AddUrlCommand = "add-url";
    public const string ActivateCommand = "activate";

    public static BrowserHandoffRequest CreateAddUrl(string url, bool startQueueAfterAdd = true) =>
        new(CurrentVersion, AddUrlCommand, url, startQueueAfterAdd);

    public static BrowserHandoffRequest CreateActivate() =>
        new(CurrentVersion, ActivateCommand, null, false);
}

public sealed record BrowserHandoffRequest(
    int Version,
    string Command,
    string? Url,
    bool StartQueueAfterAdd);

public sealed record BrowserHandoffResponse(
    int Version,
    bool Accepted,
    string Message,
    Guid? DownloadId)
{
    public static BrowserHandoffResponse Accept(string message, Guid? downloadId = null) =>
        new(BrowserHandoffProtocol.CurrentVersion, true, message, downloadId);

    public static BrowserHandoffResponse Reject(string message) =>
        new(BrowserHandoffProtocol.CurrentVersion, false, message, null);
}

public sealed record BrowserHandoffEndpointNames(string PipeName, string MutexName);

public static class BrowserHandoffEndpoint
{
    public static BrowserHandoffEndpointNames CreateForCurrentUser()
    {
        var identity = $"{Environment.UserDomainName}\\{Environment.UserName}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        var suffix = Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();

        return new BrowserHandoffEndpointNames(
            $"DownloadAja.BrowserBridge.v{BrowserHandoffProtocol.CurrentVersion}.{suffix}",
            $"Local\\DownloadAja.SingleInstance.v{BrowserHandoffProtocol.CurrentVersion}.{suffix}");
    }
}

public static class StartupCommandParser
{
    public static BrowserHandoffRequest? Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Count == 0)
        {
            return null;
        }

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (string.Equals(argument, "--add-url", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Count || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    throw new FormatException("--add-url membutuhkan URL setelah argumen.");
                }

                return BrowserHandoffProtocol.CreateAddUrl(args[index + 1].Trim());
            }

            const string prefix = "--add-url=";
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var url = argument[prefix.Length..].Trim();
                if (string.IsNullOrWhiteSpace(url))
                {
                    throw new FormatException("--add-url membutuhkan URL yang tidak kosong.");
                }

                return BrowserHandoffProtocol.CreateAddUrl(url);
            }
        }

        throw new FormatException("Argumen startup tidak dikenali. Gunakan --add-url <URL>.");
    }
}
