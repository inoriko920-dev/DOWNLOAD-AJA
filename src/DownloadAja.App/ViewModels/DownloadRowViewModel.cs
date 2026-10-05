using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using DownloadAja.Core.Downloads;

namespace DownloadAja.App.ViewModels;

public sealed class DownloadRowViewModel : INotifyPropertyChanged
{
    public DownloadRowViewModel(DownloadItem item)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DownloadItem Item { get; }
    public Guid Id => Item.Id;
    public string FileName => Item.FileName;
    public string Folder => Path.GetDirectoryName(Item.DestinationPath) ?? Item.DestinationPath;
    public string SourceUrl => Item.SourceUri.AbsoluteUri;
    public string CategoryText => Item.Category.ToString();
    public string DateText => Item.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    public string StatusText => Item.State switch
    {
        DownloadState.Waiting => "Menunggu",
        DownloadState.Downloading => Item.ProgressPercent is double progress ? $"Mengunduh {progress:0.0}%" : "Mengunduh",
        DownloadState.Paused => "Jeda",
        DownloadState.Completed => "Selesai",
        DownloadState.Stopped => "Dihentikan",
        DownloadState.Failed => "Gagal",
        _ => Item.State.ToString()
    };

    public string SizeText => Item.TotalBytes is long total
        ? $"{FormatBytes(Item.DownloadedBytes)} / {FormatBytes(total)}"
        : Item.DownloadedBytes > 0 ? FormatBytes(Item.DownloadedBytes) : "-";

    public string SpeedText => Item.SpeedBytesPerSecond is double speed && speed > 0
        ? $"{FormatBytes((long)speed)}/dtk"
        : "-";

    public string EtaText => Item.EstimatedTimeRemaining is TimeSpan eta
        ? FormatDuration(eta)
        : "-";

    public double ProgressValue => Item.ProgressPercent ?? 0d;
    public bool HasKnownProgress => Item.ProgressPercent.HasValue;
    public string ErrorText => string.IsNullOrWhiteSpace(Item.LastError) ? "Tidak ada error." : Item.LastError!;

    public void Refresh()
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(SpeedText));
        OnPropertyChanged(nameof(EtaText));
        OnPropertyChanged(nameof(ProgressValue));
        OnPropertyChanged(nameof(HasKnownProgress));
        OnPropertyChanged(nameof(ErrorText));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = Math.Max(0, bytes);
        double display = value;
        var index = 0;
        while (display >= 1024d && index < units.Length - 1)
        {
            display /= 1024d;
            index++;
        }

        return index == 0
            ? $"{display:0} {units[index]}"
            : $"{display:0.##} {units[index]}";
    }

    private static string FormatDuration(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
        }

        return $"{value.Minutes:00}:{value.Seconds:00}";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
