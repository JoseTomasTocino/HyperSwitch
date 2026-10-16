using System.Globalization;
using System.Resources;

namespace HyperSwitch.Localization;

public static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("HyperSwitch.Resources.Strings", typeof(Strings).Assembly);

    public static string AppTitle => Get();
    public static string Heading => Get();
    public static string WslAutomationName => Get();
    public static string VmAutomationName => Get();
    public static string ModeWsl => Get();
    public static string WslDescription => Get();
    public static string ModeVm => Get();
    public static string VmDescription => Get();
    public static string ActiveNow => Get();
    public static string StatusRestartRequired => Get();
    public static string Apply => Get();
    public static string Cancel => Get();
    public static string StatusPendingCanReplace => Get();
    public static string StatusPendingRestart => Get();
    public static string StatusWillApplyNextRestart => Get();
    public static string StatusCurrentMode => Get();
    public static string StatusRequestingAdmin => Get();
    public static string StatusApplyFailed => Get();
    public static string RestartPrompt => Get();
    public static string ChangeAppliedTitle => Get();
    public static string StatusUacCancelled => Get();
    public static string AdminRequestFailed => Get();
    public static string InvalidMode => Get();
    public static string ExecutableNotFound => Get();
    public static string ElevatedProcessStartFailed => Get();
    public static string RestartComment => Get();
    public static string BcdEditStartFailed => Get();
    public static string BcdEditExitFailed => Get();
    public static string ApplyConfigurationFailed => Get();
    public static string HypervisorStatusUnavailable => Get();

    private static string Get([System.Runtime.CompilerServices.CallerMemberName] string key = "") =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}
