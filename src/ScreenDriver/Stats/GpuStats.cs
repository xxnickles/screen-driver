namespace ScreenDriver.Stats;

/// <summary>
/// Reads AMD GPU usage and temperature from sysfs.
/// Usage from /sys/class/drm/card*/device/gpu_busy_percent (0–100).
/// Temperature from the "amdgpu" hwmon sensor (millidegrees Celsius).
/// </summary>
public static class GpuStats
{
    private static string? _usagePath;
    private static string? _tempPath;
    private static bool _probed;

    /// <summary>
    /// Returns GPU usage percentage (0–100), or null if not available.
    /// </summary>
    public static int? GetUsagePercent()
    {
        var path = GetUsagePath();
        if (path is null) return null;

        try
        {
            var text = File.ReadAllText(path).Trim();
            return int.Parse(text);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns GPU temperature in degrees Celsius, or null if not available.
    /// </summary>
    public static int? GetTemperatureCelsius()
    {
        var path = GetTempPath();
        if (path is null) return null;

        try
        {
            var text = File.ReadAllText(path).Trim();
            return int.Parse(text) / 1000;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetUsagePath()
    {
        if (_probed) return _usagePath;
        Probe();
        return _usagePath;
    }

    private static string? GetTempPath()
    {
        if (_probed) return _tempPath;
        Probe();
        return _tempPath;
    }

    private static void Probe()
    {
        _probed = true;

        const string drmPath = "/sys/class/drm";
        if (!Directory.Exists(drmPath)) return;

        foreach (var cardDir in Directory.GetDirectories(drmPath, "card[0-9]*"))
        {
            var usagePath = Path.Combine(cardDir, "device", "gpu_busy_percent");
            if (!File.Exists(usagePath)) continue;

            _usagePath = usagePath;

            // Find the amdgpu hwmon for temperature
            var hwmonDir = Path.Combine(cardDir, "device", "hwmon");
            if (!Directory.Exists(hwmonDir)) break;

            foreach (var hwmon in Directory.GetDirectories(hwmonDir))
            {
                var nameFile = Path.Combine(hwmon, "name");
                if (!File.Exists(nameFile)) continue;

                var name = File.ReadAllText(nameFile).Trim();
                if (name != "amdgpu") continue;

                var tempFile = Path.Combine(hwmon, "temp1_input");
                if (File.Exists(tempFile))
                    _tempPath = tempFile;

                break;
            }

            break;
        }
    }
}
