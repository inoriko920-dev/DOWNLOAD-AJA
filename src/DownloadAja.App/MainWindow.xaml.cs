using System.Windows;
using DownloadAja.Application.Downloads;
using DownloadAja.App.ViewModels;

namespace DownloadAja.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void AddUrlButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AddUrlDialog(_viewModel.DefaultDownloadDirectory)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true || dialog.Request is null)
        {
            return;
        }

        try
        {
            await _viewModel.AddDownloadAsync(dialog.Request);
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
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Unduhan tidak dapat ditambahkan.\n\n{ex.Message}",
                "Download Aja",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
