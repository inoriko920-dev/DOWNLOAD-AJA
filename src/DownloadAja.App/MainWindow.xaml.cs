using System.Windows;
using System.Windows.Input;
using DownloadAja.Application.Downloads;
using DownloadAja.Application.Settings;
using DownloadAja.App.ViewModels;

namespace DownloadAja.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly DownloadOptionsService _optionsService;

    public MainWindow(
        MainWindowViewModel viewModel,
        DownloadOptionsService optionsService)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _optionsService = optionsService ?? throw new ArgumentNullException(nameof(optionsService));
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

    private async void SchedulerButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var current = await _viewModel.GetSchedulerStateAsync();
            var dialog = new SchedulerDialog(current)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await _viewModel.ConfigureSchedulerAsync(
                dialog.SchedulerEnabled,
                dialog.StartTime,
                dialog.StopTime);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Jadwal tidak dapat diperbarui.\n\n{ex.Message}",
                "Jadwal Antrean",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void OptionsButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var current = await _optionsService.GetAsync();
            var dialog = new OptionsDialog(current)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true || dialog.Result is null)
            {
                return;
            }

            var appliedLive = await _optionsService.ApplyAsync(dialog.Result);
            var updated = await _optionsService.GetAsync();
            _viewModel.ApplyOptionsPresentation(updated, appliedLive);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Pilihan tidak dapat diperbarui.\n\n{ex.Message}",
                "Pilihan DOWNLOAD-AJA",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void FocusSearch_OnClick(object sender, RoutedEventArgs e) => FocusSearch();

    private void ClearSearch_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SearchText = string.Empty;
        FocusSearch();
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            FocusSearch();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && SearchTextBox.IsKeyboardFocusWithin && !string.IsNullOrEmpty(_viewModel.SearchText))
        {
            _viewModel.SearchText = string.Empty;
            e.Handled = true;
        }
    }

    private void FocusSearch()
    {
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }
}
