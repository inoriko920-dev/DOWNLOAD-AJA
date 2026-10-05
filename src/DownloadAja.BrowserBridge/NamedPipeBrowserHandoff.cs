using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace DownloadAja.BrowserBridge;

public sealed class NamedPipeBrowserHandoffClient
{
    private readonly string _pipeName;
    private readonly TimeSpan _connectTimeout;

    public NamedPipeBrowserHandoffClient(string pipeName, TimeSpan? connectTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        _pipeName = pipeName;
        _connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(15);
    }

    public async Task<BrowserHandoffResponse> SendAsync(
        BrowserHandoffRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var pipe = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        var timeoutMilliseconds = checked((int)Math.Clamp(
            _connectTimeout.TotalMilliseconds,
            1d,
            int.MaxValue));

        await pipe.ConnectAsync(timeoutMilliseconds, cancellationToken).ConfigureAwait(false);

        using var reader = new StreamReader(
            pipe,
            new UTF8Encoding(false),
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);
        using var writer = new StreamWriter(
            pipe,
            new UTF8Encoding(false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        var json = JsonSerializer.Serialize(request, BrowserHandoffJson.Options);
        await writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);

        var responseLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new InvalidDataException("DOWNLOAD-AJA tidak mengirim respons handoff.");
        }

        return JsonSerializer.Deserialize<BrowserHandoffResponse>(responseLine, BrowserHandoffJson.Options)
            ?? throw new InvalidDataException("Respons handoff DOWNLOAD-AJA tidak valid.");
    }
}

public sealed class NamedPipeBrowserHandoffServer
{
    private readonly string _pipeName;

    public NamedPipeBrowserHandoffServer(string pipeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        _pipeName = pipeName;
    }

    public async Task RunAsync(
        Func<BrowserHandoffRequest, CancellationToken, Task<BrowserHandoffResponse>> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        while (!cancellationToken.IsCancellationRequested)
        {
            await using var pipe = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await HandleConnectionAsync(pipe, handler, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task HandleConnectionAsync(
        Stream pipe,
        Func<BrowserHandoffRequest, CancellationToken, Task<BrowserHandoffResponse>> handler,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            pipe,
            new UTF8Encoding(false),
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);
        using var writer = new StreamWriter(
            pipe,
            new UTF8Encoding(false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        BrowserHandoffResponse response;
        try
        {
            var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                response = BrowserHandoffResponse.Reject("Permintaan handoff kosong.");
            }
            else
            {
                var request = JsonSerializer.Deserialize<BrowserHandoffRequest>(requestLine, BrowserHandoffJson.Options)
                    ?? throw new InvalidDataException("Payload handoff tidak valid.");

                response = await handler(request, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            response = BrowserHandoffResponse.Reject($"Permintaan handoff gagal: {ex.Message}");
        }

        var responseJson = JsonSerializer.Serialize(response, BrowserHandoffJson.Options);
        await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}

internal static class BrowserHandoffJson
{
    internal static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
