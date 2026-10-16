using System.Diagnostics;
using System.IO;
using System.Windows;
using HyperSwitch.Localization;
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
            ShowError(Strings.InvalidMode);
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
            throw new InvalidOperationException(Strings.ExecutableNotFound);
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = $"{ApplyArgument} {mode}",
            UseShellExecute = true,
            Verb = "runas"
        }) ?? throw new InvalidOperationException(Strings.ElevatedProcessStartFailed);

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
            Arguments = $"/r /t 5 /c \"{Strings.RestartComment}\"",
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
                throw new InvalidOperationException(Strings.BcdEditStartFailed);
            }

            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var details = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
                throw new InvalidOperationException(string.Format(
                    Strings.BcdEditExitFailed,
                    process.ExitCode,
                    details.Trim()));
            }

            return true;
        }
        catch (Exception exception)
        {
            ShowError(string.Format(Strings.ApplyConfigurationFailed, exception.Message));
            return false;
        }
    }

    private static void ShowError(string message) =>
        MessageBox.Show(message, Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
}
