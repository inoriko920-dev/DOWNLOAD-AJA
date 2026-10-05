namespace DownloadAja.Persistence.Settings;

/// <summary>
/// RECONSTRUCTED versioned settings contract. The max-connections upper bound of
/// 20 is preserved from the historical DOWNLOAD-AJA bugfix evidence.
/// </summary>
public sealed record AppSettingsSnapshot(
    int SchemaVersion,
    string DefaultDownloadDirectory,
    int MaxConnectionsPerDownload)
{
    public const int CurrentSchemaVersion = 1;
    public const int DefaultConnectionsPerDownload = 8;
    public const int MaximumConnectionsPerDownload = 20;

    public static AppSettingsSnapshot CreateDefault()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = string.IsNullOrWhiteSpace(userProfile)
            ? "Downloads"
            : Path.Combine(userProfile, "Downloads");

        return new AppSettingsSnapshot(
            CurrentSchemaVersion,
            downloads,
            DefaultConnectionsPerDownload);
    }

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Unsupported app settings schema version: {SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(DefaultDownloadDirectory))
        {
            throw new InvalidDataException("Default download directory cannot be empty.");
        }

        if (MaxConnectionsPerDownload is < 1 or > MaximumConnectionsPerDownload)
        {
            throw new InvalidDataException($"Max connections per download must be between 1 and {MaximumConnectionsPerDownload}.");
        }
    }
}
