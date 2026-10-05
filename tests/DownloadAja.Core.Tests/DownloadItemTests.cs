using DownloadAja.Core.Downloads;
using Xunit;

namespace DownloadAja.Core.Tests;

public sealed class DownloadItemTests
{
    [Fact]
    public void New_item_starts_waiting()
    {
        var item = CreateItem();

        Assert.Equal(DownloadState.Waiting, item.State);
        Assert.Equal(0, item.DownloadedBytes);
        Assert.Null(item.ProgressPercent);
    }

    [Fact]
    public void Waiting_can_transition_to_downloading_then_pause_then_resume()
    {
        var item = CreateItem();

        item.TransitionTo(DownloadState.Downloading);
        item.TransitionTo(DownloadState.Paused);
        item.TransitionTo(DownloadState.Downloading);

        Assert.Equal(DownloadState.Downloading, item.State);
    }

    [Fact]
    public void Completed_is_terminal()
    {
        var item = CreateItem();
        item.TransitionTo(DownloadState.Downloading);
        item.TransitionTo(DownloadState.Completed);

        Assert.Throws<InvalidOperationException>(() => item.TransitionTo(DownloadState.Downloading));
    }

    [Fact]
    public void Failed_requires_error_message()
    {
        var item = CreateItem();

        Assert.Throws<ArgumentException>(() => item.TransitionTo(DownloadState.Failed));
    }

    [Fact]
    public void Progress_is_calculated_from_bytes()
    {
        var item = CreateItem();
        item.TransitionTo(DownloadState.Downloading);
        item.UpdateProgress(50, 200, 1024, TimeSpan.FromSeconds(10));

        Assert.Equal(25d, item.ProgressPercent);
        Assert.Equal(1024d, item.SpeedBytesPerSecond);
    }

    [Fact]
    public void Downloaded_bytes_cannot_exceed_total()
    {
        var item = CreateItem();

        Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdateProgress(201, 200, null, null));
    }

    private static DownloadItem CreateItem() => new(
        new Uri("https://example.com/file.iso"),
        "file.iso",
        @"C:\Downloads\file.iso");
}
