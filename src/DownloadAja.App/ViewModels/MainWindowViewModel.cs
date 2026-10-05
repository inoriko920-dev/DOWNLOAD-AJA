using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Threading;
using DownloadAja.Application.Downloads;
using DownloadAja.Application.Queue;
using DownloadAja.Application.Scheduler;
using DownloadAja.Application.Settings;
using DownloadAja.Core.Downloads;
using DownloadAja.Persistence.Scheduler;

namespace DownloadAja.App.ViewModels;

/// <summary>
/// RECONSTRUCTED main-window presentation layer. It binds the historical IDM-like
/// shell to the real queue coordinator, scheduler, search/filtering, and periodically
/// refreshed aria2 state.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DownloadQueueCoordinator _queue;
    private readonly AddDownloadService _addDownloadService;
    private readonly QueueSchedulerService _scheduler;
    private readonly DispatcherTimer _refreshTimer;
    private readonly Dictionary<Guid, DownloadRowViewModel> _rowsById = new();
    private DownloadRowViewModel? _selectedDownload;
    private CategoryFilterOption? _selectedCategory;
    private string _searchText = string.Empty;
    private string _statusText = "Memuat...";
    private string _summaryText = "Unduhan aktif: 0   |   Total kecepatan: 0 B/dtk";
    private string _schedulerStatusText = "Jadwal nonaktif";
    private bool _refreshInProgress;
    private bool _disposed;

    public MainWindowViewModel(
        DownloadQueueCoordinator queue,
        AddDownloadService addDownloadService,
        QueueSchedulerService scheduler,
        string defaultDownloadDirectory)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _addDownloadService = addDownloadService ?? throw new ArgumentNullException(nameof(addDownloadService));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        DefaultDownloadDirectory = string.IsNullOrWhiteSpace(defaultDownloadDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            : defaultDownloadDirectory;

        Categories = new ObservableCollection<CategoryFilterOption>
        {
            new("all", "Semua Unduhan"),
            new("completed", "Selesai"),
            new("unfinished", "Belum Selesai"),
            new("queue", "Antrean"),
            new("video", "Video"),
            new("audio", "Audio"),
            new("document", "Dokumen"),
            new("archive", "Arsip"),
            new("program", "Program")
        };

        Downloads = new ObservableCollection<DownloadRowViewModel>();
        DownloadsView = CollectionViewSource.GetDefaultView(Downloads);
        DownloadsView.Filter = FilterDownload;

        StartCommand = new AsyncRelayCommand(StartSelectedOrQueueAsync, CanStart, SetCommandError);
        PauseCommand = new AsyncRelayCommand(PauseSelectedAsync, CanPause, SetCommandError);
        StopCommand = new AsyncRelayCommand(StopSelectedAsync, CanStop, SetCommandError);
        RestartCommand = new AsyncRelayCommand(RestartSelectedAsync, CanRestart, SetCommandError);
        StopAllCommand = new AsyncRelayCommand(StopAllAsync, () => Downloads.Count > 0, SetCommandError);
        RefreshCommand = new AsyncRelayCommand(RefreshNowAsync, () => !_refreshInProgress, SetCommandError);

        SelectedCategory = Categories[0];

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(750)
        };
        _refreshTimer.Tick += RefreshTimerOnTick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DownloadRowViewModel> Downloads { get; }
    public ICollectionView DownloadsView { get; }
    public ObservableCollection<CategoryFilterOption> Categories { get; }
    public string DefaultDownloadDirectory { get; private set; }

    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand PauseCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand RestartCommand { get; }
    public AsyncRelayCommand StopAllCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    public DownloadRowViewModel? SelectedDownload
    {
        get => _selectedDownload;
        set
        {
            if (ReferenceEquals(_selectedDownload, value))
            {
                return;
            }

            _selectedDownload = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedProgressText));
            RaiseCommandStates();
        }
    }

    public CategoryFilterOption? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (Equals(_selectedCategory, value))
            {
                return;
            }

            _selectedCategory = value;
            OnPropertyChanged();
            DownloadsView.Refresh();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_searchText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _searchText = normalized;
            OnPropertyChanged();
            DownloadsView.Refresh();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string SummaryText
    {
        get => _summaryText;
        private set
        {
            if (_summaryText == value)
            {
                return;
            }

            _summaryText = value;
            OnPropertyChanged();
        }
    }

    public string SchedulerStatusText
    {
        get => _schedulerStatusText;
        private set
        {
            if (_schedulerStatusText == value)
            {
                return;
            }

            _schedulerStatusText = value;
            OnPropertyChanged();
        }
    }

    public string SelectedProgressText => SelectedDownload is null
        ? "Belum ada unduhan yang dipilih."
        : $"{SelectedDownload.StatusText} — {SelectedDownload.SizeText}";

    public async Task InitializeAsync()
    {
        await _queue.InitializeAsync();
        await _scheduler.InitializeAsync();
        await RefreshSchedulerStatusAsync();
        await RefreshFromCoordinatorAsync(refreshEngine: false);
        _refreshTimer.Start();
        StatusText = "Siap";
    }

    public Task<SchedulerStateSnapshot> GetSchedulerStateAsync() => _scheduler.GetStateAsync();

    public async Task ConfigureSchedulerAsync(bool enabled, TimeSpan startTime, TimeSpan stopTime)
    {
        await _scheduler.ConfigureAsync(enabled, startTime, stopTime);
        await RefreshSchedulerStatusAsync();
        await RefreshFromCoordinatorAsync(refreshEngine: false);
        StatusText = enabled ? "Jadwal antrean diperbarui" : "Jadwal antrean dinonaktifkan";
    }

    public void ApplyOptionsPresentation(DownloadOptionsSnapshot options, bool speedAppliedLive)
    {
        ArgumentNullException.ThrowIfNull(options);
        DefaultDownloadDirectory = options.DefaultDownloadDirectory;
        OnPropertyChanged(nameof(DefaultDownloadDirectory));

        var speedText = options.GlobalDownloadLimitBytesPerSecond == 0
            ? "tanpa batas"
            : FormatBytesPerSecond(options.GlobalDownloadLimitBytesPerSecond);
        var applyText = speedAppliedLive ? "diterapkan langsung" : "tersimpan untuk runtime aria2 berikutnya";
        StatusText = $"Pilihan diperbarui — batas kecepatan {speedText}, {applyText}";
    }

    public async Task<AddDownloadResult> AddDownloadAsync(AddDownloadRequest request)
    {
        var result = await _addDownloadService.AddAsync(request);
        await RefreshFromCoordinatorAsync(refreshEngine: false);

        if (_rowsById.TryGetValue(result.Item.Id, out var row))
        {
            SelectedCategory = Categories[0];
            SearchText = string.Empty;
            SelectedDownload = row;
        }

        StatusText = result.Item.State == DownloadState.Downloading
            ? $"Mengunduh {result.Item.FileName}"
            : $"Ditambahkan ke antrean: {result.Item.FileName}";

        return result;
    }

    public async Task RefreshNowAsync()
    {
        await _scheduler.EvaluateAsync();
        await RefreshFromCoordinatorAsync(refreshEngine: true);
    }

    private async void RefreshTimerOnTick(object? sender, EventArgs e)
    {
        if (_refreshInProgress || _disposed)
        {
            return;
        }

        try
        {
            await _scheduler.EvaluateAsync();
            await RefreshFromCoordinatorAsync(refreshEngine: true);
        }
        catch (Exception ex)
        {
            SetCommandError(ex);
        }
    }

    private async Task RefreshSchedulerStatusAsync()
    {
        var state = await _scheduler.GetStateAsync();
        SchedulerStatusText = state.Enabled
            ? $"Jadwal aktif {FormatTime(state.StartTime)}–{FormatTime(state.StopTime)}"
            : "Jadwal nonaktif";
    }

    private async Task RefreshFromCoordinatorAsync(bool refreshEngine)
    {
        if (_refreshInProgress)
        {
            return;
        }

        _refreshInProgress = true;
        RefreshCommand.RaiseCanExecuteChanged();
        try
        {
            if (refreshEngine)
            {
                await _queue.RefreshAsync();
            }

            var snapshot = await _queue.GetSnapshotAsync();
            ApplySnapshot(snapshot);
        }
        finally
        {
            _refreshInProgress = false;
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    private void ApplySnapshot(DownloadQueueSnapshot snapshot)
    {
        var orderedIds = snapshot.Items.Select(static item => item.Id).ToHashSet();

        foreach (var staleId in _rowsById.Keys.Where(id => !orderedIds.Contains(id)).ToArray())
        {
            var row = _rowsById[staleId];
            Downloads.Remove(row);
            _rowsById.Remove(staleId);
            if (ReferenceEquals(SelectedDownload, row))
            {
                SelectedDownload = null;
            }
        }

        for (var index = 0; index < snapshot.Items.Count; index++)
        {
            var item = snapshot.Items[index];
            if (!_rowsById.TryGetValue(item.Id, out var row))
            {
                row = new DownloadRowViewModel(item);
                _rowsById.Add(item.Id, row);
                Downloads.Insert(Math.Min(index, Downloads.Count), row);
            }
            else
            {
                var currentIndex = Downloads.IndexOf(row);
                if (currentIndex >= 0 && currentIndex != index)
                {
                    Downloads.Move(currentIndex, index);
                }
            }

            row.Refresh();
        }

        DownloadsView.Refresh();
        var totalSpeed = snapshot.Items.Sum(static item => item.SpeedBytesPerSecond ?? 0d);
        SummaryText = $"Unduhan aktif: {snapshot.ActiveDownloads}   |   Total kecepatan: {FormatBytesPerSecond(totalSpeed)}   |   {SchedulerStatusText}";
        StatusText = snapshot.IsRunning ? "Antrean berjalan" : "Siap";
        OnPropertyChanged(nameof(SelectedProgressText));
        RaiseCommandStates();
    }

    private bool FilterDownload(object value)
    {
        if (value is not DownloadRowViewModel row)
        {
            return false;
        }

        var categoryMatch = SelectedCategory?.Key switch
        {
            null or "all" => true,
            "completed" => row.Item.State == DownloadState.Completed,
            "unfinished" => row.Item.State != DownloadState.Completed,
            "queue" => row.Item.State is DownloadState.Waiting or DownloadState.Downloading or DownloadState.Paused,
            "video" => row.Item.Category == DownloadCategory.Video,
            "audio" => row.Item.Category == DownloadCategory.Audio,
            "document" => row.Item.Category == DownloadCategory.Document,
            "archive" => row.Item.Category == DownloadCategory.Archive,
            "program" => row.Item.Category == DownloadCategory.Program,
            _ => true
        };

        return categoryMatch && DownloadSearchMatcher.Matches(row.Item, SearchText);
    }

    private bool CanStart() => SelectedDownload is null || SelectedDownload.Item.State is DownloadState.Waiting or DownloadState.Paused or DownloadState.Stopped or DownloadState.Failed;
    private bool CanPause() => SelectedDownload?.Item.State == DownloadState.Downloading;
    private bool CanStop() => SelectedDownload?.Item.State is DownloadState.Waiting or DownloadState.Downloading or DownloadState.Paused or DownloadState.Failed;
    private bool CanRestart() => SelectedDownload?.Item.State is DownloadState.Stopped or DownloadState.Failed;

    private async Task StartSelectedOrQueueAsync()
    {
        if (SelectedDownload?.Item.State == DownloadState.Paused)
        {
            await _queue.ResumeAsync(SelectedDownload.Id);
        }
        else if (SelectedDownload?.Item.State is DownloadState.Stopped or DownloadState.Failed)
        {
            await _queue.RestartAsync(SelectedDownload.Id);
            await _queue.StartQueueAsync();
        }
        else
        {
            await _queue.StartQueueAsync();
        }

        await RefreshFromCoordinatorAsync(refreshEngine: true);
    }

    private async Task PauseSelectedAsync()
    {
        if (SelectedDownload is null)
        {
            return;
        }

        await _queue.PauseAsync(SelectedDownload.Id);
        await RefreshFromCoordinatorAsync(refreshEngine: true);
    }

    private async Task StopSelectedAsync()
    {
        if (SelectedDownload is null)
        {
            return;
        }

        await _queue.StopAsync(SelectedDownload.Id);
        await RefreshFromCoordinatorAsync(refreshEngine: true);
    }

    private async Task RestartSelectedAsync()
    {
        if (SelectedDownload is null)
        {
            return;
        }

        await _queue.RestartAsync(SelectedDownload.Id);
        await RefreshFromCoordinatorAsync(refreshEngine: false);
    }

    private async Task StopAllAsync()
    {
        await _queue.StopAllAsync();
        await RefreshFromCoordinatorAsync(refreshEngine: false);
    }

    private void SetCommandError(Exception exception)
    {
        StatusText = $"Error: {exception.Message}";
    }

    private void RaiseCommandStates()
    {
        StartCommand.RaiseCanExecuteChanged();
        PauseCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        RestartCommand.RaiseCanExecuteChanged();
        StopAllCommand.RaiseCanExecuteChanged();
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
    {
        string[] units = ["B/dtk", "KB/dtk", "MB/dtk", "GB/dtk"];
        var value = Math.Max(0d, bytesPerSecond);
        var index = 0;
        while (value >= 1024d && index < units.Length - 1)
        {
            value /= 1024d;
            index++;
        }

        return index == 0 ? $"{value:0} {units[index]}" : $"{value:0.##} {units[index]}";
    }

    private static string FormatTime(TimeSpan value) => $"{value.Hours:00}:{value.Minutes:00}";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _refreshTimer.Stop();
        _refreshTimer.Tick -= RefreshTimerOnTick;
    }
}
