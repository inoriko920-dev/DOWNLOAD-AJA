using DownloadAja.Core.Downloads;

namespace DownloadAja.Application.Downloads;

/// <summary>
/// RECONSTRUCTED case-insensitive search helper for IDM-like history filtering.
/// Search intentionally stays side-effect free so WPF filtering can be tested
/// independently from the presentation layer.
/// </summary>
public static class DownloadSearchMatcher
{
    public static bool Matches(DownloadItem item, string? query)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length == 0)
        {
            return true;
        }

        var searchable = string.Join('\n',
            item.FileName,
            item.SourceUri.AbsoluteUri,
            item.DestinationPath,
            item.State.ToString(),
            item.Category.ToString(),
            item.LastError ?? string.Empty);

        return terms.All(term =>
            searchable.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
