using System.Windows;
using HyperSwitch.Services;

namespace HyperSwitch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (ElevatedCommandHandler.TryHandle(e.Args, out var exitCode))
        {
            Shutdown(exitCode);
            return;
        }

        new MainWindow().Show();
    }
}
