using System.IO;
using System.Windows;
using DownloadAja.App.ViewModels;
using DownloadAja.Application.Downloads;
using DownloadAja.Application.Queue;
using DownloadAja.Infrastructure.Aria2;
using DownloadAja.Persistence.Downloads;
using DownloadAja.Persistence.Queue;
using DownloadAja.Persistence.Settings;

namespace DownloadAja.App;

public partial class App : System.Windows.Application
{
    private Aria2ProcessManager? _aria2Runtime;
    private MainWindowViewModel? _mainViewModel;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DownloadAja",
                "recovery-v1");
            Directory.CreateDirectory(dataDirectory);

            var settingsStore = new JsonAppSettingsStore(Path.Combine(dataDirectory, "settings.json"));
            var settings = await settingsStore.LoadAsync();

            var aria2Options = Aria2Options.CreateDefault(
                AppContext.BaseDirectory,
                splitCount: settings.MaxConnectionsPerDownload);
            var rpcClient = new Aria2RpcClient(aria2Options);
            _aria2Runtime = new Aria2ProcessManager(aria2Options, rpcClient);
            var engine = new Aria2DownloadEngine(_aria2Runtime, rpcClient);

            var downloadStore = new JsonDownloadStore(Path.Combine(dataDirectory, "downloads.json"));
            var queueStore = new JsonQueueStateStore(Path.Combine(dataDirectory, "queue.json"));
            var coordinator = new DownloadQueueCoordinator(engine, downloadStore, queueStore);
            var addDownloadService = new AddDownloadService(coordinator);

            _mainViewModel = new MainWindowViewModel(
                coordinator,
                addDownloadService,
                settings.DefaultDownloadDirectory);
            await _mainViewModel.InitializeAsync();

            var window = new MainWindow(_mainViewModel);
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"DOWNLOAD-AJA tidak dapat dimulai.\n\n{ex.Message}",
                "Download Aja",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();

        if (_aria2Runtime is not null)
        {
            await _aria2Runtime.DisposeAsync();
        }

        base.OnExit(e);
    }
}
