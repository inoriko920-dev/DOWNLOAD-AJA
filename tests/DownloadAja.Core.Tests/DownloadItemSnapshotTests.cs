using DownloadAja.Core.Downloads;
using Xunit;

namespace DownloadAja.Core.Tests;

public sealed class DownloadItemSnapshotTests
{
    [Theory]
    [InlineData("movie.mkv", DownloadCategory.Video)]
    [InlineData("song.mp3", DownloadCategory.Audio)]
    [InlineData("report.pdf", DownloadCategory.Document)]
    [InlineData("archive.zip", DownloadCategory.Archive)]
    [InlineData("setup.exe", DownloadCategory.Program)]
    [InlineData("disk.iso", DownloadCategory.Other)]
    public void Category_is_inferred_from_extension(string fileName, DownloadCategory expected)
    {
        Assert.Equal(expected, DownloadItem.InferCategory(fileName));
    }

    [Fact]
    public void Snapshot_round_trip_preserves_recoverable_state()
    {
        var item = new DownloadItem(
            new Uri("https://example.com/video.mp4"),
            "video.mp4",
            @"C:\Downloads\video.mp4");

        item.TransitionTo(DownloadState.Downloading);
        item.UpdateProgress(512, 1024, 2048, TimeSpan.FromSeconds(1));
        item.TransitionTo(DownloadState.Paused);

        var snapshot = DownloadItemSnapshot.FromItem(item);
        var restored = snapshot.ToItem();

        Assert.Equal(item.Id, restored.Id);
        Assert.Equal(item.SourceUri, restored.SourceUri);
        Assert.Equal(item.FileName, restored.FileName);
        Assert.Equal(item.DestinationPath, restored.DestinationPath);
        Assert.Equal(item.Category, restored.Category);
        Assert.Equal(item.State, restored.State);
        Assert.Equal(item.DownloadedBytes, restored.DownloadedBytes);
        Assert.Equal(item.TotalBytes, restored.TotalBytes);
        Assert.Equal(item.SpeedBytesPerSecond, restored.SpeedBytesPerSecond);
        Assert.Equal(item.EstimatedTimeRemaining, restored.EstimatedTimeRemaining);
        Assert.Equal(item.CreatedAt, restored.CreatedAt);
    }

    [Fact]
    public void Unsupported_snapshot_schema_is_rejected()
    {
        var snapshot = new DownloadItemSnapshot(
            SchemaVersion: 999,
            Id: Guid.NewGuid(),
            SourceUri: "https://example.com/file.bin",
            FileName: "file.bin",
            DestinationPath: @"C:\Downloads\file.bin",
            Category: DownloadCategory.Other,
            State: DownloadState.Waiting,
            DownloadedBytes: 0,
            TotalBytes: null,
            SpeedBytesPerSecond: null,
            EstimatedTimeRemaining: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow);

        Assert.Throws<NotSupportedException>(() => snapshot.ToItem());
    }
}
