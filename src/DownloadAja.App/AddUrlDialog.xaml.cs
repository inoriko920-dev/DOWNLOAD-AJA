using System.IO;
using System.Windows;
using DownloadAja.Application.Downloads;
using Microsoft.Win32;

namespace DownloadAja.App;

public partial class AddUrlDialog : Window
{
    public AddUrlDialog(string defaultDownloadDirectory)
    {
        InitializeComponent();
        FolderBox.Text = defaultDownloadDirectory ?? string.Empty;
        Loaded += (_, _) => UrlBox.Focus();
    }

    public AddDownloadRequest? Request { get; private set; }

    private void UrlBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(FileNameBox.Text))
        {
            return;
        }

        try
        {
            var uri = AddDownloadService.ParseSourceUri(UrlBox.Text);
            FileNameBox.Text = DownloadFileNameResolver.Suggest(uri);
        }
        catch (AddDownloadValidationException)
        {
            // Validation is shown only when the user confirms the dialog.
        }
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Pilih folder tujuan DOWNLOAD-AJA",
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

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var uri = AddDownloadService.ParseSourceUri(UrlBox.Text);
            var fileName = AddDownloadService.ResolveFileName(uri, FileNameBox.Text);

            if (string.IsNullOrWhiteSpace(FolderBox.Text))
            {
                throw new AddDownloadValidationException("Folder tujuan tidak boleh kosong.");
            }

            Request = new AddDownloadRequest(
                uri.AbsoluteUri,
                FolderBox.Text.Trim(),
                fileName,
                StartQueueCheckBox.IsChecked == true);

            DialogResult = true;
        }
        catch (AddDownloadValidationException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Tambah URL",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
