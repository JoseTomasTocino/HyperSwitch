using System.Runtime.InteropServices;
using HyperSwitch.Models;

namespace HyperSwitch.Services;

public static class HypervisorStateDetector
{
    // PF_HYPERVISOR_PRESENT: Windows reports whether a hypervisor is active now.
    private const uint PfHypervisorPresent = 28;

    public static HypervisorMode GetCurrentMode() =>
        IsProcessorFeaturePresent(PfHypervisorPresent)
            ? HypervisorMode.Wsl
            : HypervisorMode.VirtualMachines;

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsProcessorFeaturePresent(uint processorFeature);
}
