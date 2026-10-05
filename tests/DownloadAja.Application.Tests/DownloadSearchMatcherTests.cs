using DownloadAja.Application.Downloads;
using DownloadAja.Core.Downloads;
using Xunit;

namespace DownloadAja.Application.Tests;

public sealed class DownloadSearchMatcherTests
{
    [Fact]
    public void Empty_query_matches_every_item()
    {
        var item = CreateItem();

        Assert.True(DownloadSearchMatcher.Matches(item, null));
        Assert.True(DownloadSearchMatcher.Matches(item, "   "));
    }

    [Theory]
    [InlineData("movie")]
    [InlineData("EXAMPLE.COM")]
    [InlineData("downloads")]
    [InlineData("video")]
    [InlineData("waiting")]
    public void Query_matches_supported_fields_case_insensitively(string query)
    {
        var item = CreateItem();

        Assert.True(DownloadSearchMatcher.Matches(item, query));
    }

    [Fact]
    public void Multiple_terms_use_and_semantics()
    {
        var item = CreateItem();

        Assert.True(DownloadSearchMatcher.Matches(item, "movie example.com"));
        Assert.False(DownloadSearchMatcher.Matches(item, "movie missing-term"));
    }

    [Fact]
    public void Error_text_is_searchable()
    {
        var item = CreateItem();
        item.TransitionTo(DownloadState.Failed, "Server returned 403 forbidden");

        Assert.True(DownloadSearchMatcher.Matches(item, "403 forbidden"));
    }

    private static DownloadItem CreateItem() => new(
        new Uri("https://example.com/media/movie.mp4"),
        "movie.mp4",
        @"C:\Downloads\movie.mp4");
}
