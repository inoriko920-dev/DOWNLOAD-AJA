using System.Buffers.Binary;
using System.Text.Json;

namespace DownloadAja.BrowserBridge;

/// <summary>
/// Chrome native messaging uses a 4-byte little-endian length prefix followed
/// by one UTF-8 JSON payload. The native host must never print arbitrary text to
/// stdout because stdout is the protocol channel.
/// </summary>
public static class NativeMessagingFraming
{
    public const int MaxMessageBytes = 1024 * 1024;

    public static async Task<T?> ReadAsync<T>(
        Stream input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var lengthBytes = new byte[sizeof(int)];
        var headerRead = await ReadExactlyOrEofAsync(
            input,
            lengthBytes,
            cancellationToken).ConfigureAwait(false);

        if (!headerRead)
        {
            return default;
        }

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
        if (length <= 0 || length > MaxMessageBytes)
        {
            throw new InvalidDataException(
                $"Panjang native message tidak valid: {length} byte.");
        }

        var payload = new byte[length];
        await input.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);

        var message = JsonSerializer.Deserialize<T>(payload, NativeMessagingJson.Options);
        if (message is null)
        {
            throw new InvalidDataException("Payload native message tidak valid.");
        }

        return message;
    }

    public static async Task WriteAsync<T>(
        Stream output,
        T message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, NativeMessagingJson.Options);
        if (payload.Length == 0 || payload.Length > MaxMessageBytes)
        {
            throw new InvalidDataException(
                $"Ukuran native message tidak valid: {payload.Length} byte.");
        }

        var lengthBytes = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, payload.Length);

        await output.WriteAsync(lengthBytes, cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ReadExactlyOrEofAsync(
        Stream input,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await input.ReadAsync(
                buffer[totalRead..],
                cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                if (totalRead == 0)
                {
                    return false;
                }

                throw new EndOfStreamException(
                    "Native messaging stream berakhir di tengah header.");
            }

            totalRead += read;
        }

        return true;
    }
}

internal static class NativeMessagingJson
{
    internal static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}
