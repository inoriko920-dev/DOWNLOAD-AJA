using DownloadAja.Persistence.Settings;
using Xunit;

namespace DownloadAja.Persistence.Tests;

public sealed class AppSettingsCompatibilityTests
{
    [Fact]
    public async Task Speed_limit_round_trip_is_preserved()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        var store = new JsonAppSettingsStore(path);
        var settings = new AppSettingsSnapshot(
            AppSettingsSnapshot.CurrentSchemaVersion,
            @"C:\Downloads",
            MaxConnectionsPerDownload: 12,
            GlobalDownloadLimitBytesPerSecond: 5 * 1024 * 1024);

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equal(5 * 1024 * 1024, loaded.GlobalDownloadLimitBytesPerSecond);
    }

    [Fact]
    public async Task Existing_schema_v1_json_without_speed_limit_loads_as_unlimited()
    {
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "settings.json");
        await File.WriteAllTextAsync(
            path,
            """
            {
              "schemaVersion": 1,
              "defaultDownloadDirectory": "C:\\Downloads",
              "maxConnectionsPerDownload": 8
            }
            """);

        var loaded = await new JsonAppSettingsStore(path).LoadAsync();

        Assert.Equal(0, loaded.GlobalDownloadLimitBytesPerSecond);
        Assert.Equal(8, loaded.MaxConnectionsPerDownload);
    }

    [Fact]
    public void Negative_speed_limit_is_rejected()
    {
        var settings = new AppSettingsSnapshot(
            AppSettingsSnapshot.CurrentSchemaVersion,
            @"C:\Downloads",
            MaxConnectionsPerDownload: 8,
            GlobalDownloadLimitBytesPerSecond: -1);

        Assert.Throws<InvalidDataException>(settings.Validate);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DownloadAja.Settings.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
