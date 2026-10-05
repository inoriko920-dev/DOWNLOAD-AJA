using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DownloadAja.Infrastructure.Aria2;

/// <summary>
/// RECONSTRUCTED JSON-RPC 2.0 client for a local aria2 process.
/// </summary>
public sealed class Aria2RpcClient : IAria2RpcClient
{
    private readonly Aria2Options _options;
    private readonly HttpClient _httpClient;
    private long _requestId;

    public Aria2RpcClient(Aria2Options options, HttpClient? httpClient = null)
    {
        _options = options.Validated();
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await CallAsync("aria2.getVersion", Array.Empty<object?>(), cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or Aria2RpcException or JsonException)
        {
            return false;
        }
    }

    public async Task<string> AddUriAsync(Uri sourceUri, string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!sourceUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Download URI must be absolute.", nameof(sourceUri));
        }

        var fullDestinationPath = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullDestinationPath)
            ?? throw new ArgumentException("Destination path must include a directory.", nameof(destinationPath));
        var fileName = Path.GetFileName(fullDestinationPath);

        var rpcOptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["dir"] = directory,
            ["out"] = fileName,
            ["continue"] = "true",
            ["auto-file-renaming"] = "false",
            ["split"] = _options.SplitCount.ToString(CultureInfo.InvariantCulture),
            ["max-connection-per-server"] = _options.MaxConnectionsPerServer.ToString(CultureInfo.InvariantCulture)
        };

        var result = await CallAsync(
            "aria2.addUri",
            new object?[] { new[] { sourceUri.AbsoluteUri }, rpcOptions },
            cancellationToken).ConfigureAwait(false);

        return result.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(result.GetString())
            ? result.GetString()!
            : throw new InvalidDataException("aria2.addUri did not return a valid GID.");
    }

    public async Task PauseAsync(string gid, CancellationToken cancellationToken = default)
    {
        ValidateGid(gid);
        _ = await CallAsync("aria2.pause", new object?[] { gid }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ResumeAsync(string gid, CancellationToken cancellationToken = default)
    {
        ValidateGid(gid);
        _ = await CallAsync("aria2.unpause", new object?[] { gid }, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string gid, CancellationToken cancellationToken = default)
    {
        ValidateGid(gid);
        _ = await CallAsync("aria2.remove", new object?[] { gid }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Aria2Status> TellStatusAsync(string gid, CancellationToken cancellationToken = default)
    {
        ValidateGid(gid);

        var keys = new[]
        {
            "gid",
            "status",
            "totalLength",
            "completedLength",
            "downloadSpeed",
            "errorMessage"
        };

        var result = await CallAsync("aria2.tellStatus", new object?[] { gid, keys }, cancellationToken).ConfigureAwait(false);
        if (result.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("aria2.tellStatus returned an invalid result.");
        }

        return new Aria2Status(
            ReadRequiredString(result, "gid"),
            ReadRequiredString(result, "status"),
            ReadInt64(result, "completedLength"),
            ReadNullableInt64(result, "totalLength"),
            ReadInt64(result, "downloadSpeed"),
            ReadOptionalString(result, "errorMessage"));
    }

    public async Task ChangeGlobalDownloadLimitAsync(long bytesPerSecond, CancellationToken cancellationToken = default)
    {
        if (bytesPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesPerSecond));
        }

        var rpcOptions = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["max-overall-download-limit"] = bytesPerSecond.ToString(CultureInfo.InvariantCulture)
        };

        _ = await CallAsync(
            "aria2.changeGlobalOption",
            new object?[] { rpcOptions },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _ = await CallAsync("aria2.shutdown", Array.Empty<object?>(), cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonElement> CallAsync(string method, object?[] methodParameters, CancellationToken cancellationToken)
    {
        var parameters = new object?[methodParameters.Length + 1];
        parameters[0] = $"token:{_options.RpcSecret}";
        Array.Copy(methodParameters, 0, parameters, 1, methodParameters.Length);

        var request = new RpcRequest(
            "2.0",
            Interlocked.Increment(ref _requestId).ToString(CultureInfo.InvariantCulture),
            method,
            parameters);

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(_options.RpcEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
        {
            var code = error.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsedCode)
                ? parsedCode
                : -1;
            var message = error.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString() ?? "Unknown aria2 RPC error"
                : "Unknown aria2 RPC error";
            throw new Aria2RpcException(code, message);
        }

        if (!root.TryGetProperty("result", out var result))
        {
            throw new InvalidDataException("aria2 RPC response did not contain a result.");
        }

        return result.Clone();
    }

    private static string ReadRequiredString(JsonElement element, string propertyName)
    {
        var value = ReadOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException($"aria2 status is missing '{propertyName}'.")
            : value;
    }

    private static string? ReadOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static long ReadInt64(JsonElement element, string propertyName)
    {
        var value = ReadOptionalString(element, propertyName);
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidDataException($"aria2 status property '{propertyName}' is not a valid integer.");
    }

    private static long? ReadNullableInt64(JsonElement element, string propertyName)
    {
        var value = ReadOptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value) || value == "0")
        {
            return null;
        }

        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new InvalidDataException($"aria2 status property '{propertyName}' is not a valid integer.");
    }

    private static void ValidateGid(string gid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gid);
    }

    private sealed record RpcRequest(
        [property: JsonPropertyName("jsonrpc")] string JsonRpc,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("params")] object?[] Parameters);
}
