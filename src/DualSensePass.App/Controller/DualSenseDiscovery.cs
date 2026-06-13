using HidSharp;

namespace DualSensePass.Controller;

public static class DualSenseDiscovery
{
    private const int SonyVendorId = 0x054C;

    private static readonly HashSet<int> DualSenseProductIds = new()
    {
        0x0CE6, // DualSense
        0x0DF2  // DualSense Edge
    };

    public static DualSenseDeviceInfo? FindFirst()
    {
        var device = DeviceList.Local.GetHidDevices(SonyVendorId)
            .Where(d => DualSenseProductIds.Contains(d.ProductID))
            .OrderByDescending(d => d.GetMaxInputReportLength())
            .FirstOrDefault();

        if (device is null)
        {
            return null;
        }

        var connection = GuessConnection(device);
        var productName = SafeGetProductName(device);
        return new DualSenseDeviceInfo(device, $"{productName} ({connection})", connection);
    }

    private static DualSenseConnection GuessConnection(HidDevice device)
    {
        var path = device.DevicePath?.ToLowerInvariant() ?? string.Empty;
        return path.Contains("bthenum") || path.Contains("bluetooth")
            ? DualSenseConnection.Bluetooth
            : DualSenseConnection.Usb;
    }

    private static string SafeGetProductName(HidDevice device)
    {
        try
        {
            return device.GetProductName();
        }
        catch
        {
            return device.ProductID == 0x0DF2 ? "DualSense Edge" : "DualSense";
        }
    }
}
