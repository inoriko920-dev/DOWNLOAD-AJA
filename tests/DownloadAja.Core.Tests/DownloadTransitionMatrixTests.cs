using DownloadAja.Core.Downloads;
using Xunit;

namespace DownloadAja.Core.Tests;

public sealed class DownloadTransitionMatrixTests
{
    [Fact]
    public void Transition_matrix_matches_recovery_contract()
    {
        var allowed = new HashSet<(DownloadState From, DownloadState To)>
        {
            (DownloadState.Waiting, DownloadState.Downloading),
            (DownloadState.Waiting, DownloadState.Stopped),
            (DownloadState.Waiting, DownloadState.Failed),
            (DownloadState.Downloading, DownloadState.Paused),
            (DownloadState.Downloading, DownloadState.Completed),
            (DownloadState.Downloading, DownloadState.Stopped),
            (DownloadState.Downloading, DownloadState.Failed),
            (DownloadState.Paused, DownloadState.Downloading),
            (DownloadState.Paused, DownloadState.Stopped),
            (DownloadState.Paused, DownloadState.Failed),
            (DownloadState.Failed, DownloadState.Waiting),
            (DownloadState.Failed, DownloadState.Downloading),
            (DownloadState.Failed, DownloadState.Stopped),
            (DownloadState.Stopped, DownloadState.Waiting),
            (DownloadState.Stopped, DownloadState.Downloading)
        };

        foreach (var from in Enum.GetValues<DownloadState>())
        {
            foreach (var to in Enum.GetValues<DownloadState>())
            {
                var item = RestoreInState(from);
                var error = to == DownloadState.Failed ? "network error" : null;

                if (allowed.Contains((from, to)))
                {
                    item.TransitionTo(to, error);
                    Assert.Equal(to, item.State);
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => item.TransitionTo(to, error));
                }
            }
        }
    }

    private static DownloadItem RestoreInState(DownloadState state) => DownloadItem.Restore(
        Guid.NewGuid(),
        new Uri("https://example.com/file.bin"),
        "file.bin",
        @"C:\Downloads\file.bin",
        DownloadCategory.Other,
        state,
        downloadedBytes: 0,
        totalBytes: null,
        speedBytesPerSecond: null,
        estimatedTimeRemaining: null,
        lastError: state == DownloadState.Failed ? "existing error" : null,
        createdAt: DateTimeOffset.UtcNow);
}
