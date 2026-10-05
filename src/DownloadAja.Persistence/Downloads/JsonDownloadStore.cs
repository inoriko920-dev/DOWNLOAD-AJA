using System.Collections.Concurrent;
using System.Text.Json;
using DownloadAja.Core.Downloads;

namespace DownloadAja.Persistence.Downloads;

/// <summary>
/// RECONSTRUCTED JSON persistence with serialized access and same-directory atomic
/// replacement. The design specifically guards against the historical save-race
/// defect recorded during DOWNLOAD-AJA recovery.
/// </summary>
public sealed class JsonDownloadStore : IDownloadStore
{
    private const int StoreSchemaVersion = 1;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _filePath;
    private readonly string _tempPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _gate;

    public JsonDownloadStore(string filePath)
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

    public async Task<IReadOnlyList<DownloadItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
            {
                if (!File.Exists(_tempPath))
                {
                    return Array.Empty<DownloadItem>();
                }

                var recovered = await ReadDocumentAsync(_tempPath, cancellationToken).ConfigureAwait(false);
                PromoteRecoveredTemp();
                return recovered;
            }

            try
            {
                return await ReadDocumentAsync(_filePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (IsRecoverableReadFailure(ex) && File.Exists(_tempPath))
            {
                var recovered = await ReadDocumentAsync(_tempPath, cancellationToken).ConfigureAwait(false);
                PromoteRecoveredTemp();
                return recovered;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IReadOnlyCollection<DownloadItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var document = new DownloadStoreDocument
        {
            SchemaVersion = StoreSchemaVersion,
            Items = items.Select(DownloadItemSnapshot.FromItem).ToArray()
        };

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
                await JsonSerializer.SerializeAsync(stream, document, _jsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            ReplaceAtomically();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<DownloadItem>> ReadDocumentAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var document = await JsonSerializer.DeserializeAsync<DownloadStoreDocument>(stream, _jsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (document is null)
        {
            throw new InvalidDataException("Download store is empty or invalid.");
        }

        if (document.SchemaVersion != StoreSchemaVersion)
        {
            throw new NotSupportedException($"Unsupported download store schema version: {document.SchemaVersion}.");
        }

        if (document.Items is null)
        {
            throw new InvalidDataException("Download store does not contain an item collection.");
        }

        return document.Items.Select(static snapshot => snapshot.ToItem()).ToArray();
    }

    private void ReplaceAtomically()
    {
        if (File.Exists(_filePath))
        {
            File.Replace(_tempPath, _filePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            return;
        }

        File.Move(_tempPath, _filePath);
    }

    private void PromoteRecoveredTemp()
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

    private sealed class DownloadStoreDocument
    {
        public int SchemaVersion { get; init; }
        public DownloadItemSnapshot[]? Items { get; init; }
    }
}
