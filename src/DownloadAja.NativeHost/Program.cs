using System.Diagnostics;
using DownloadAja.BrowserBridge;

namespace DownloadAja.NativeHost;

internal static class Program
{
    private static async Task<int> Main()
    {
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();

        try
        {
            while (true)
            {
                var request = await NativeMessagingFraming.ReadAsync<NativeHostRequest>(input);
                if (request is null)
                {
                    return 0;
                }

                var response = await HandleAsync(request);
                await NativeMessagingFraming.WriteAsync(output, response);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"DOWNLOAD-AJA native host berhenti: {ex.Message}");
            return 1;
        }
    }

    private static async Task<NativeHostResponse> HandleAsync(NativeHostRequest request)
    {
        if (request.Version != NativeMessagingProtocol.CurrentVersion)
        {
            return NativeHostResponse.Reject(
                $"Versi native messaging tidak didukung: {request.Version}.");
        }

        if (string.Equals(
                request.Command,
                NativeMessagingProtocol.PingCommand,
                StringComparison.OrdinalIgnoreCase))
        {
            return NativeHostResponse.Accept(
                $"DOWNLOAD-AJA native host v{NativeMessagingProtocol.CurrentVersion} siap.",
                desktopReachable: await CanReachDesktopAsync().ConfigureAwait(false));
        }

        if (!string.Equals(
                request.Command,
                NativeMessagingProtocol.AddUrlCommand,
                StringComparison.OrdinalIgnoreCase))
        {
            return NativeHostResponse.Reject(
                $"Perintah native messaging tidak didukung: {request.Command}.");
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return NativeHostResponse.Reject("Extension tidak mengirim URL.");
        }

        var desktopRequest = BrowserHandoffProtocol.CreateAddUrl(
            request.Url,
            request.StartQueueAfterAdd);

        var firstAttempt = await TryForwardAsync(
            desktopRequest,
            TimeSpan.FromMilliseconds(600)).ConfigureAwait(false);
        if (firstAttempt is not null)
        {
            return FromDesktopResponse(firstAttempt);
        }

        var launchResult = TryLaunchDesktop();
        if (!launchResult.Started)
        {
            return NativeHostResponse.Reject(launchResult.ErrorMessage);
        }

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(300).ConfigureAwait(false);

            var response = await TryForwardAsync(
                desktopRequest,
                TimeSpan.FromMilliseconds(900)).ConfigureAwait(false);
            if (response is not null)
            {
                return FromDesktopResponse(response);
            }
        }

        return NativeHostResponse.Reject(
            "DOWNLOAD-AJA berhasil dijalankan, tetapi belum siap menerima URL dalam 20 detik.");
    }

    private static async Task<bool> CanReachDesktopAsync()
    {
        try
        {
            var endpoint = BrowserHandoffEndpoint.CreateForCurrentUser();
            var client = new NamedPipeBrowserHandoffClient(
                endpoint.PipeName,
                TimeSpan.FromMilliseconds(350));
            var response = await client.SendAsync(BrowserHandoffProtocol.CreateActivate()).ConfigureAwait(false);
            return response.Accepted;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static async Task<BrowserHandoffResponse?> TryForwardAsync(
        BrowserHandoffRequest request,
        TimeSpan connectTimeout)
    {
        try
        {
            var endpoint = BrowserHandoffEndpoint.CreateForCurrentUser();
            var client = new NamedPipeBrowserHandoffClient(endpoint.PipeName, connectTimeout);
            return await client.SendAsync(request).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static NativeHostResponse FromDesktopResponse(BrowserHandoffResponse response) =>
        new(
            NativeMessagingProtocol.CurrentVersion,
            response.Accepted,
            response.Message,
            response.DownloadId,
            DesktopReachable: true);

    private static DesktopLaunchResult TryLaunchDesktop()
    {
        var configuredPath = Environment.GetEnvironmentVariable("DOWNLOAD_AJA_DESKTOP_PATH");
        var desktopPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(AppContext.BaseDirectory, "Download Aja.exe")
            : Path.GetFullPath(configuredPath);

        if (!File.Exists(desktopPath))
        {
            return new DesktopLaunchResult(
                false,
                $"Download Aja.exe tidak ditemukan di: {desktopPath}. Jalankan installer integrasi browser dari folder portable yang benar.");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = desktopPath,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(desktopPath) ?? AppContext.BaseDirectory
            });

            return new DesktopLaunchResult(true, string.Empty);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new DesktopLaunchResult(
                false,
                $"DOWNLOAD-AJA tidak dapat dijalankan: {ex.Message}");
        }
    }

    private sealed record DesktopLaunchResult(bool Started, string ErrorMessage);
}
