namespace DownloadAja.Application.Settings;

/// <summary>
/// RECONSTRUCTED options exposed by the desktop Options dialog.
/// GlobalDownloadLimitBytesPerSecond = 0 means unlimited.
/// </summary>
public sealed record DownloadOptionsSnapshot(
    string DefaultDownloadDirectory,
    int MaxConnectionsPerDownload,
    int MaxSimultaneousDownloads,
    long GlobalDownloadLimitBytesPerSecond)
{
    public const int MaximumSimultaneousDownloads = 20;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultDownloadDirectory))
        {
            throw new ArgumentException("Folder unduhan default tidak boleh kosong.", nameof(DefaultDownloadDirectory));
        }

        if (MaxConnectionsPerDownload is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxConnectionsPerDownload), "Koneksi per unduhan harus antara 1 dan 20.");
        }

        if (MaxSimultaneousDownloads is < 1 or > MaximumSimultaneousDownloads)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSimultaneousDownloads), $"Unduhan bersamaan harus antara 1 dan {MaximumSimultaneousDownloads}.");
        }

        if (GlobalDownloadLimitBytesPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(GlobalDownloadLimitBytesPerSecond), "Batas kecepatan tidak boleh negatif.");
        }
    }
}
