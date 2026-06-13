using Microsoft.Win32;

namespace DualBox.Diagnostics;

public sealed record PrerequisiteStatus(bool ViGEmBusInstalled, bool HidHideInstalled, IReadOnlyList<string> Warnings)
{
    public string ToSummary()
    {
        var vigem = ViGEmBusInstalled ? "ViGEmBus found" : "ViGEmBus missing";
        var hidhide = HidHideInstalled ? "HidHide found" : "HidHide missing";
        return $"{vigem}; {hidhide}";
    }
}

public static class PrerequisiteChecker
{
    public static PrerequisiteStatus Check()
    {
        var vigem = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ViGEmBus") is not null;
        var hidhide = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\HidHide") is not null;
        var warnings = new List<string>();

        if (!vigem)
        {
            warnings.Add("ViGEmBus is required so Windows games see a virtual Xbox controller.");
        }

        if (!hidhide)
        {
            warnings.Add("HidHide is recommended so games do not also see the physical DualSense.");
        }

        return new PrerequisiteStatus(vigem, hidhide, warnings);
    }
}
