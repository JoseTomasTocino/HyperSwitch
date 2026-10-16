using System.ComponentModel;
using System.Windows;
using HyperSwitch.Models;
using HyperSwitch.Services;

namespace HyperSwitch;

public partial class MainWindow : Window
{
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
        var configuredMode = _configuredMode ?? _currentMode;
        var changed = SelectedMode != configuredMode;
        ApplyButton.IsEnabled = changed;

        if (_configuredMode.HasValue && _configuredMode.Value != _currentMode)
        {
            StatusText.Text = changed
                ? "Hay un cambio pendiente. Puedes sustituirlo antes de reiniciar."
                : "Cambio guardado. Reinicia Windows para activar el nuevo modo.";
            return;
        }

        StatusText.Text = changed
            ? "El cambio se aplicará en el próximo reinicio de Windows."
            : "Este es el modo activo actualmente.";
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var requestedMode = SelectedMode;
            SetControlsEnabled(false);
            StatusText.Text = "Solicitando permisos de administrador…";

            var exitCode = await ElevatedCommandHandler.ApplyElevatedAsync(requestedMode);
            if (exitCode != 0)
            {
                StatusText.Text = "No se pudo aplicar el cambio.";
                return;
            }

            _configuredMode = requestedMode;
            UpdateSelectionState();

            var restart = MessageBox.Show(
                "El cambio se ha guardado correctamente, pero necesita un reinicio para surtir efecto.\n\n" +
                "¿Quieres reiniciar Windows ahora?",
                "Cambio aplicado",
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
            StatusText.Text = "Se canceló la solicitud de permisos de administrador.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "No se pudo solicitar permisos de administrador.\n\n" + exception.Message,
                "HyperSwitch",
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

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
