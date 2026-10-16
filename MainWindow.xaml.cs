using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Imaging;
using HyperSwitch.Localization;
using HyperSwitch.Models;
using HyperSwitch.Services;

namespace HyperSwitch;

public partial class MainWindow : Window
{
    private static readonly BitmapImage WslRockerImage = LoadRockerImage("rocker-wsl.png");
    private static readonly BitmapImage VmRockerImage = LoadRockerImage("rocker-vm.png");

    private readonly HypervisorMode _currentMode;
    private HypervisorMode? _configuredMode;
    private bool _isInitializing = true;

    public MainWindow()
    {
        InitializeComponent();

        _currentMode = HypervisorStateDetector.GetCurrentMode();
        WslModeOption.IsChecked = _currentMode == HypervisorMode.Wsl;
        VmModeOption.IsChecked = _currentMode == HypervisorMode.VirtualMachines;
        WslActiveBadge.Visibility = _currentMode == HypervisorMode.Wsl ? Visibility.Visible : Visibility.Collapsed;
        VmActiveBadge.Visibility = _currentMode == HypervisorMode.VirtualMachines ? Visibility.Visible : Visibility.Collapsed;

        _isInitializing = false;
        UpdateSelectionState();
    }

    private HypervisorMode SelectedMode =>
        WslModeOption.IsChecked == true ? HypervisorMode.Wsl : HypervisorMode.VirtualMachines;

    private void ModeOption_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing && IsLoaded)
        {
            UpdateSelectionState();
        }
    }

    private void UpdateSelectionState()
    {
        RockerImage.Source = SelectedMode == HypervisorMode.Wsl ? WslRockerImage : VmRockerImage;

        var configuredMode = _configuredMode ?? _currentMode;
        var changed = SelectedMode != configuredMode;
        ApplyButton.IsEnabled = changed;

        if (_configuredMode.HasValue && _configuredMode.Value != _currentMode)
        {
            StatusText.Text = changed
                ? Strings.StatusPendingCanReplace
                : Strings.StatusPendingRestart;
            return;
        }

        StatusText.Text = changed
            ? Strings.StatusWillApplyNextRestart
            : Strings.StatusCurrentMode;
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var requestedMode = SelectedMode;
            SetControlsEnabled(false);
            StatusText.Text = Strings.StatusRequestingAdmin;

            var exitCode = await ElevatedCommandHandler.ApplyElevatedAsync(requestedMode);
            if (exitCode != 0)
            {
                StatusText.Text = Strings.StatusApplyFailed;
                return;
            }

            _configuredMode = requestedMode;
            UpdateSelectionState();

            var restart = MessageBox.Show(
                Strings.RestartPrompt,
                Strings.ChangeAppliedTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Information,
                MessageBoxResult.No);

            if (restart == MessageBoxResult.Yes)
            {
                ElevatedCommandHandler.RestartComputer();
                Close();
            }
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            StatusText.Text = Strings.StatusUacCancelled;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                string.Format(Strings.AdminRequestFailed, exception.Message),
                Strings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            SetControlsEnabled(true);
            UpdateSelectionState();
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        WslModeOption.IsEnabled = enabled;
        VmModeOption.IsEnabled = enabled;
        ApplyButton.IsEnabled = enabled;
    }

    private static BitmapImage LoadRockerImage(string fileName)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/Assets/{fileName}", UriKind.Absolute);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
