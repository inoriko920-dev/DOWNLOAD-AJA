using DownloadAja.Persistence.Internal;

namespace DownloadAja.Persistence.Settings;

/// <summary>
/// RECONSTRUCTED atomic application-settings persistence.
/// </summary>
public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private readonly AtomicJsonFile<AppSettingsSnapshot> _file;

    public JsonAppSettingsStore(string filePath)
    {
        _file = new AtomicJsonFile<AppSettingsSnapshot>(filePath);
    }

    public async Task<AppSettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _file.LoadAsync(cancellationToken).ConfigureAwait(false)
            ?? AppSettingsSnapshot.CreateDefault();
        settings.Validate();
        return settings;
    }

    public Task SaveAsync(AppSettingsSnapshot settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        return _file.SaveAsync(settings, cancellationToken);
    }
}
