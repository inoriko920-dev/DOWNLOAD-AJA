namespace DownloadAja.Core.Downloads;

/// <summary>
/// RECONSTRUCTED recovery model. Historical UI states are mapped onto a small,
/// deterministic domain state machine so engine/UI behavior can be tested.
/// </summary>
public enum DownloadState
{
    Waiting = 0,
    Downloading = 1,
    Paused = 2,
    Completed = 3,
    Stopped = 4,
    Failed = 5
}
