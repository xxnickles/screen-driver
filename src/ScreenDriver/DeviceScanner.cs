namespace ScreenDriver;

/// <summary>
/// Scans Linux sysfs to find our USB screen by its vendor/product IDs.
/// This is hardcoded to the QinHeng Electronics UsbMonitor (VID 1a86, PID 5722).
/// </summary>
public static class DeviceScanner
{
    private const string SysUsbPath = "/sys/bus/usb/devices";
    private const string ExpectedVendorId = "1a86";
    private const string ExpectedProductId = "5722";

    /// <summary>
    /// Scans all USB devices and returns the /dev/ttyACM* path for our screen,
    /// or null if not found.
    /// </summary>
    public static string? FindScreen()
    {
        if (!Directory.Exists(SysUsbPath))
            return null;

        foreach (var deviceDir in Directory.GetDirectories(SysUsbPath))
        {
            var vendorFile = Path.Combine(deviceDir, "idVendor");
            var productFile = Path.Combine(deviceDir, "idProduct");

            if (!File.Exists(vendorFile) || !File.Exists(productFile))
                continue;

            var vendor = File.ReadAllText(vendorFile).Trim();
            var product = File.ReadAllText(productFile).Trim();

            if (vendor != ExpectedVendorId || product != ExpectedProductId)
                continue;

            // Found our device — now find the tty port name
            var portName = FindTtyUnder(deviceDir);
            if (portName is not null)
                return $"/dev/{portName}";
        }

        return null;
    }

    /// <summary>
    /// Looks for a "tty" folder inside the device's interface subdirectories.
    /// The sysfs layout is: device/interface/tty/ttyACM0
    /// e.g., /sys/bus/usb/devices/5-1/5-1:1.0/tty/ttyACM0
    /// We avoid recursive search because sysfs contains symlinks that cause hangs.
    /// </summary>
    private static string? FindTtyUnder(string deviceDir)
    {
        try
        {
            foreach (var interfaceDir in Directory.GetDirectories(deviceDir))
            {
                var ttyDir = Path.Combine(interfaceDir, "tty");
                if (!Directory.Exists(ttyDir))
                    continue;

                var entries = Directory.GetDirectories(ttyDir);
                if (entries.Length > 0)
                    return Path.GetFileName(entries[0]);
            }

            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
