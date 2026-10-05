using System.Globalization;
using System.IO;
using System.Windows;
using DownloadAja.Application.Settings;
using Microsoft.Win32;

namespace DownloadAja.App;

public partial class OptionsDialog : Window
{
    public OptionsDialog(DownloadOptionsSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        InitializeComponent();

        FolderBox.Text = current.DefaultDownloadDirectory;
        ConnectionsBox.Text = current.MaxConnectionsPerDownload.ToString(CultureInfo.InvariantCulture);
        SimultaneousBox.Text = current.MaxSimultaneousDownloads.ToString(CultureInfo.InvariantCulture);
        SpeedLimitBox.Text = (current.GlobalDownloadLimitBytesPerSecond / 1024L).ToString(CultureInfo.InvariantCulture);
    }

    public DownloadOptionsSnapshot? Result { get; private set; }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Pilih folder unduhan default DOWNLOAD-AJA",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(FolderBox.Text) && Directory.Exists(FolderBox.Text))
        {
            dialog.InitialDirectory = FolderBox.Text;
        }

        if (dialog.ShowDialog(this) == true)
        {
            FolderBox.Text = dialog.FolderName;
        }
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(ConnectionsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var connections))
            {
                throw new ArgumentException("Koneksi per unduhan harus berupa angka 1–20.");
            }

            if (!int.TryParse(SimultaneousBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var simultaneous))
            {
                throw new ArgumentException("Unduhan bersamaan harus berupa angka 1–20.");
            }

            if (!long.TryParse(SpeedLimitBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var speedLimitKb) || speedLimitKb < 0)
            {
                throw new ArgumentException("Batas kecepatan harus berupa angka 0 atau lebih besar.");
            }

            long speedLimitBytes;
            try
            {
                speedLimitBytes = checked(speedLimitKb * 1024L);
            }
            catch (OverflowException)
            {
                throw new ArgumentException("Batas kecepatan terlalu besar.");
            }

            var snapshot = new DownloadOptionsSnapshot(
                FolderBox.Text.Trim(),
                connections,
                simultaneous,
                speedLimitBytes);
            snapshot.Validate();

            Result = snapshot;
            DialogResult = true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Pilihan DOWNLOAD-AJA",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
