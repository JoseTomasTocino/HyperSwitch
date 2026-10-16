using System.Management;
using HyperSwitch.Localization;
using HyperSwitch.Models;

namespace HyperSwitch.Services;

public static class HypervisorStateDetector
{
    public static HypervisorMode GetCurrentMode()
    {
        using var searcher = new ManagementObjectSearcher(
            "root\\CIMV2",
            "SELECT HypervisorPresent FROM Win32_ComputerSystem");
        using var systems = searcher.Get();

        foreach (ManagementObject system in systems)
        {
            using (system)
            {
                if (system["HypervisorPresent"] is bool hypervisorPresent)
                {
                    return hypervisorPresent
                        ? HypervisorMode.Wsl
                        : HypervisorMode.VirtualMachines;
                }
            }
        }

        throw new InvalidOperationException(Strings.HypervisorStatusUnavailable);
    }
}
