namespace DownloadAja.Application.Downloads;

/// <summary>
/// RECONSTRUCTED filename resolution for manually added HTTP/HTTPS URLs.
/// It intentionally stays deterministic and does not perform a network request.
/// </summary>
public static class DownloadFileNameResolver
{
    public static string Suggest(Uri sourceUri)
    {
        ArgumentNullException.ThrowIfNull(sourceUri);

        var escapedName = Path.GetFileName(sourceUri.AbsolutePath);
        var candidate = string.IsNullOrWhiteSpace(escapedName)
            ? "download"
            : Uri.UnescapeDataString(escapedName);

        return Sanitize(candidate);
    }

    public static string Sanitize(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(fileName
            .Trim()
            .Select(ch => invalid.Contains(ch) || char.IsControl(ch) ? '_' : ch)
            .ToArray())
            .TrimEnd('.', ' ');

        if (string.IsNullOrWhiteSpace(sanitized) || sanitized is "." or "..")
        {
            return "download";
        }

        return sanitized;
    }
}
