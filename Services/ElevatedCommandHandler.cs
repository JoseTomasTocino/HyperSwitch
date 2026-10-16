using System.Diagnostics;
using System.IO;
using System.Windows;
using HyperSwitch.Models;

namespace HyperSwitch.Services;

public static class ElevatedCommandHandler
{
    private const string ApplyArgument = "--apply";

    public static bool TryHandle(string[] arguments, out int exitCode)
    {
        exitCode = 0;

        if (arguments.Length != 2 || !arguments[0].Equals(ApplyArgument, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Enum.TryParse<HypervisorMode>(arguments[1], ignoreCase: true, out var mode))
        {
            ShowError("HyperSwitch recibió un modo de hipervisor no válido.");
            exitCode = 2;
            return true;
        }

        exitCode = Apply(mode) ? 0 : 1;
        return true;
    }

    public static async Task<int> ApplyElevatedAsync(HypervisorMode mode)
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("No se pudo localizar el ejecutable de HyperSwitch.");
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = $"{ApplyArgument} {mode}",
            UseShellExecute = true,
            Verb = "runas"
        }) ?? throw new InvalidOperationException("No se pudo iniciar el proceso con permisos de administrador.");

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    public static void RestartComputer()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var shutdownPath = Path.Combine(windowsDirectory, "System32", "shutdown.exe");

        Process.Start(new ProcessStartInfo
        {
            FileName = shutdownPath,
            Arguments = "/r /t 5 /c \"HyperSwitch aplicó el nuevo modo de hipervisor.\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    private static bool Apply(HypervisorMode mode)
    {
        var launchType = mode == HypervisorMode.Wsl ? "Auto" : "Off";
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var bcdEditPath = Path.Combine(windowsDirectory, "System32", "bcdedit.exe");

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = bcdEditPath,
                Arguments = $"/set {{current}} hypervisorlaunchtype {launchType}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process is null)
            {
                throw new InvalidOperationException("No se pudo iniciar BCDEdit.");
            }

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var details = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
                throw new InvalidOperationException(
                    $"BCDEdit terminó con el código {process.ExitCode}.\n\n{details.Trim()}");
            }

            return true;
        }
        catch (Exception exception)
        {
            ShowError(
                "No se pudo cambiar la configuración de arranque.\n\n" +
                exception.Message);
            return false;
        }
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, "HyperSwitch", MessageBoxButton.OK, MessageBoxImage.Error);
}
