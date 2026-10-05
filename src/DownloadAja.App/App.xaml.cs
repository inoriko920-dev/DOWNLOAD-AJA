using System.IO;
using System.Windows;
using DownloadAja.App.ViewModels;
using DownloadAja.Application.Downloads;
using DownloadAja.Application.Queue;
using DownloadAja.Application.Scheduler;
using DownloadAja.Application.Settings;
using DownloadAja.BrowserBridge;
using DownloadAja.Infrastructure.Aria2;
using DownloadAja.Persistence.Downloads;
using DownloadAja.Persistence.Queue;
using DownloadAja.Persistence.Scheduler;
using DownloadAja.Persistence.Settings;

namespace DownloadAja.App;

public partial class App : System.Windows.Application
{
    private Aria2ProcessManager? _aria2Runtime;
    private MainWindowViewModel? _mainViewModel;
    private IBrowserDownloadHandoff? _browserHandoff;
    private SingleInstanceGuard? _singleInstance;
    private CancellationTokenSource? _handoffCancellation;
    private Task? _handoffServerTask;
    private BrowserHandoffEndpointNames? _handoffEndpoint;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        BrowserHandoffRequest? startupRequest;
        try
        {
            startupRequest = StartupCommandParser.Parse(e.Args);
        }
        catch (FormatException ex)
        {
            MessageBox.Show(
                ex.Message,
                "Download Aja",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown(2);
            return;
        }

        _handoffEndpoint = BrowserHandoffEndpoint.CreateForCurrentUser();
        _singleInstance = SingleInstanceGuard.Acquire(_handoffEndpoint.MutexName);

        if (!_singleInstance.IsPrimary)
        {
            await ForwardToPrimaryAndExitAsync(
                startupRequest ?? BrowserHandoffProtocol.CreateActivate());
            return;
        }

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
                splitCount: settings.MaxConnectionsPerDownload,
                globalDownloadLimitBytesPerSecond: settings.GlobalDownloadLimitBytesPerSecond);
            var rpcClient = new Aria2RpcClient(aria2Options);
            _aria2Runtime = new Aria2ProcessManager(aria2Options, rpcClient);
            var engine = new Aria2DownloadEngine(_aria2Runtime, rpcClient);

            var downloadStore = new JsonDownloadStore(Path.Combine(dataDirectory, "downloads.json"));
            var queueStore = new JsonQueueStateStore(Path.Combine(dataDirectory, "queue.json"));
            var schedulerStore = new JsonSchedulerStateStore(Path.Combine(dataDirectory, "scheduler.json"));
            var coordinator = new DownloadQueueCoordinator(engine, downloadStore, queueStore);
            var scheduler = new QueueSchedulerService(coordinator, schedulerStore);
            var addDownloadService = new AddDownloadService(coordinator);
            var optionsService = new DownloadOptionsService(
                settingsStore,
                coordinator,
                aria2Options,
                rpcClient);
            _browserHandoff = new BrowserDownloadHandoffService(
                addDownloadService,
                settingsStore);

            _mainViewModel = new MainWindowViewModel(
                coordinator,
                addDownloadService,
                scheduler,
                settings.DefaultDownloadDirectory);
            await _mainViewModel.InitializeAsync();

            var window = new MainWindow(_mainViewModel, optionsService);
            MainWindow = window;
            window.Show();

            _handoffCancellation = new CancellationTokenSource();
            var server = new NamedPipeBrowserHandoffServer(_handoffEndpoint.PipeName);
            _handoffServerTask = server.RunAsync(
                HandleHandoffAsync,
                _handoffCancellation.Token);

            if (startupRequest is not null)
            {
                var startupResponse = await HandleHandoffAsync(
                    startupRequest,
                    _handoffCancellation.Token);

                if (!startupResponse.Accepted)
                {
                    MessageBox.Show(
                        startupResponse.Message,
                        "Tambah URL",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
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

    private async Task ForwardToPrimaryAndExitAsync(BrowserHandoffRequest request)
    {
        try
        {
            var client = new NamedPipeBrowserHandoffClient(
                _handoffEndpoint!.PipeName,
                TimeSpan.FromSeconds(20));
            var response = await client.SendAsync(request);

            if (!response.Accepted)
            {
                MessageBox.Show(
                    response.Message,
                    "Download Aja",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            Shutdown(response.Accepted ? 0 : 3);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Tidak dapat meneruskan permintaan ke DOWNLOAD-AJA yang sedang berjalan.\n\n{ex.Message}",
                "Download Aja",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(4);
        }
    }

    private async Task<BrowserHandoffResponse> HandleHandoffAsync(
        BrowserHandoffRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Version != BrowserHandoffProtocol.CurrentVersion)
        {
            return BrowserHandoffResponse.Reject(
                $"Versi protocol tidak didukung: {request.Version}.");
        }

        if (string.Equals(
                request.Command,
                BrowserHandoffProtocol.ActivateCommand,
                StringComparison.OrdinalIgnoreCase))
        {
            await Dispatcher.InvokeAsync(ActivateMainWindow, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken);
            return BrowserHandoffResponse.Accept("DOWNLOAD-AJA diaktifkan.");
        }

        if (_browserHandoff is null)
        {
            return BrowserHandoffResponse.Reject("DOWNLOAD-AJA belum siap menerima URL.");
        }

        var response = await _browserHandoff.HandleAsync(request, cancellationToken);
        if (response.Accepted)
        {
            await Dispatcher.InvokeAsync(ActivateMainWindow, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken);
        }

        return response;
    }

    private void ActivateMainWindow()
    {
        if (MainWindow is not Window window)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
        window.Focus();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _handoffCancellation?.Cancel();
        if (_handoffServerTask is not null)
        {
            try
            {
                await _handoffServerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }

        _mainViewModel?.Dispose();

        if (_aria2Runtime is not null)
        {
            await _aria2Runtime.DisposeAsync();
        }

        _handoffCancellation?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
