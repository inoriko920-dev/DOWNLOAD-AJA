using System.Collections.Concurrent;
using System.Text.Json;

namespace DownloadAja.Persistence.Internal;

/// <summary>
/// RECONSTRUCTED same-directory atomic JSON helper used by recovery persistence.
/// Multiple store instances targeting the same file share one in-process gate.
/// </summary>
internal sealed class AtomicJsonFile<T> where T : class
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _filePath;
    private readonly string _tempPath;
    private readonly SemaphoreSlim _gate;
    private readonly JsonSerializerOptions _jsonOptions;

    public AtomicJsonFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = Path.GetFullPath(filePath);
        _tempPath = _filePath + ".tmp";
        _gate = FileLocks.GetOrAdd(_filePath, static _ => new SemaphoreSlim(1, 1));
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
    }

    public async Task<T?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
            {
                if (!File.Exists(_tempPath))
                {
                    return null;
                }

                var recovered = await ReadAsync(_tempPath, cancellationToken).ConfigureAwait(false);
                PromoteTemp();
                return recovered;
            }

            try
            {
                return await ReadAsync(_filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsRecoverableReadFailure(ex) && File.Exists(_tempPath))
            {
                var recovered = await ReadAsync(_tempPath, cancellationToken).ConfigureAwait(false);
                PromoteTemp();
                return recovered;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(T value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using (var stream = new FileStream(
                _tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, value, _jsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            PromoteTemp();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<T> ReadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var value = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
        return value ?? throw new InvalidDataException("JSON persistence file is empty or invalid.");
    }

    private void PromoteTemp()
    {
        if (!File.Exists(_tempPath))
        {
            return;
        }

        if (File.Exists(_filePath))
        {
            File.Replace(_tempPath, _filePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return;
        }

        File.Move(_tempPath, _filePath);
    }

    private static bool IsRecoverableReadFailure(Exception exception) => exception is
        JsonException or
        InvalidDataException or
        NotSupportedException or
        IOException;
}
