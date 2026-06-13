using HidSharp;

namespace DualBox.Controller;

public sealed record DualSenseDeviceInfo(HidDevice Device, string DisplayName, DualSenseConnection Connection);

public enum DualSenseConnection
{
    Usb,
    Bluetooth
}
