using System.Globalization;
using System.Windows;
using DownloadAja.Persistence.Scheduler;

namespace DownloadAja.App;

public partial class SchedulerDialog : Window
{
    public SchedulerDialog(SchedulerStateSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        InitializeComponent();

        EnabledCheckBox.IsChecked = current.Enabled;
        StartTimeTextBox.Text = FormatTime(current.StartTime);
        StopTimeTextBox.Text = FormatTime(current.StopTime);
    }

    public bool SchedulerEnabled { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan StopTime { get; private set; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        ValidationText.Text = string.Empty;

        if (!TryParseTime(StartTimeTextBox.Text, out var startTime))
        {
            ValidationText.Text = "Waktu mulai tidak valid. Gunakan format HH:mm, misalnya 08:00.";
            StartTimeTextBox.Focus();
            return;
        }

        if (!TryParseTime(StopTimeTextBox.Text, out var stopTime))
        {
            ValidationText.Text = "Waktu berhenti tidak valid. Gunakan format HH:mm, misalnya 22:00.";
            StopTimeTextBox.Focus();
            return;
        }

        if (startTime == stopTime)
        {
            ValidationText.Text = "Waktu mulai dan berhenti tidak boleh sama.";
            return;
        }

        SchedulerEnabled = EnabledCheckBox.IsChecked == true;
        StartTime = startTime;
        StopTime = stopTime;
        DialogResult = true;
    }

    private static bool TryParseTime(string? value, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                value.Trim(),
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return false;
        }

        result = new TimeSpan(parsed.Hour, parsed.Minute, 0);
        return true;
    }

    private static string FormatTime(TimeSpan value) =>
        $"{value.Hours:00}:{value.Minutes:00}";
}
