using DownloadAja.Application.Queue;
using DownloadAja.Core.Downloads;

namespace DownloadAja.Application.Downloads;

/// <summary>
/// RECONSTRUCTED R7 application service for adding a real download into the
/// persisted queue. Validation and duplicate detection live outside WPF so they
/// are testable and reusable by browser handoff later.
/// </summary>
public sealed class AddDownloadService
{
    private readonly DownloadQueueCoordinator _queue;

    public AddDownloadService(DownloadQueueCoordinator queue)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    public async Task<AddDownloadResult> AddAsync(
        AddDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sourceUri = ParseSourceUri(request.Url);
        var destinationDirectory = NormalizeDirectory(request.DestinationDirectory);
        var fileName = ResolveFileName(sourceUri, request.FileName);
        var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, fileName));

        EnsureDestinationInsideDirectory(destinationDirectory, destinationPath);
        Directory.CreateDirectory(destinationDirectory);

        var snapshot = await _queue.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        ValidateDuplicate(snapshot, sourceUri, destinationPath);

        if (File.Exists(destinationPath))
        {
            throw new AddDownloadValidationException(
                $"File tujuan sudah ada: {destinationPath}. Ubah nama file atau folder tujuan.");
        }

        var item = new DownloadItem(sourceUri, fileName, destinationPath);
        await _queue.AddAsync(item, cancellationToken).ConfigureAwait(false);

        if (request.StartQueueAfterAdd)
        {
            await _queue.StartQueueAsync(cancellationToken).ConfigureAwait(false);
        }

        return new AddDownloadResult(item, request.StartQueueAfterAdd);
    }

    public static Uri ParseSourceUri(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            throw new AddDownloadValidationException("URL tidak boleh kosong.");
        }

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
        {
            throw new AddDownloadValidationException("URL tidak valid.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new AddDownloadValidationException("Baseline recovery saat ini hanya menerima URL HTTP atau HTTPS.");
        }

        return uri;
    }

    public static string ResolveFileName(Uri sourceUri, string? requestedFileName)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);

        if (string.IsNullOrWhiteSpace(requestedFileName))
        {
            return DownloadFileNameResolver.Suggest(sourceUri);
        }

        var requested = requestedFileName.Trim();
        if (Path.GetFileName(requested) != requested)
        {
            throw new AddDownloadValidationException("Nama file tidak boleh berisi path folder.");
        }

        var sanitized = DownloadFileNameResolver.Sanitize(requested);
        if (!string.Equals(sanitized, requested.TrimEnd('.', ' '), StringComparison.Ordinal))
        {
            throw new AddDownloadValidationException("Nama file berisi karakter yang tidak valid untuk Windows.");
        }

        return sanitized;
    }

    private static string NormalizeDirectory(string rawDirectory)
    {
        if (string.IsNullOrWhiteSpace(rawDirectory))
        {
            throw new AddDownloadValidationException("Folder tujuan tidak boleh kosong.");
        }

        try
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(rawDirectory.Trim()));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new AddDownloadValidationException($"Folder tujuan tidak valid: {ex.Message}");
        }
    }

    private static void EnsureDestinationInsideDirectory(string directory, string destinationPath)
    {
        var normalizedDirectory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory))
            + Path.DirectorySeparatorChar;
        var normalizedDestination = Path.GetFullPath(destinationPath);

        if (!normalizedDestination.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new AddDownloadValidationException("Path file tujuan keluar dari folder yang dipilih.");
        }
    }

    private static void ValidateDuplicate(
        DownloadQueueSnapshot snapshot,
        Uri sourceUri,
        string destinationPath)
    {
        foreach (var existing in snapshot.Items)
        {
            if (string.Equals(existing.DestinationPath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new AddDownloadValidationException(
                    $"Sudah ada unduhan dengan file tujuan yang sama: {existing.FileName}.");
            }

            if (Uri.Compare(
                    existing.SourceUri,
                    sourceUri,
                    UriComponents.HttpRequestUrl,
                    UriFormat.SafeUnescaped,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new AddDownloadValidationException(
                    $"URL ini sudah ada di daftar unduhan: {existing.FileName}.");
            }
        }
    }
}
