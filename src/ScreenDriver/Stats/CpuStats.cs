namespace ScreenDriver.Stats;

/// <summary>
/// Reads CPU usage from /proc/stat using delta-based jiffy comparison.
/// Reads CPU temperature from the k10temp hwmon sensor (AMD) if available.
/// </summary>
public static class CpuStats
{
    private static long _prevIdle;
    private static long _prevTotal;
    private static string? _tempPath;
    private static bool _tempProbed;

    /// <summary>
    /// Returns overall CPU usage as a percentage (0–100).
    /// First call returns 0 (no baseline yet).
    /// </summary>
    public static double GetUsagePercent()
    {
        var line = File.ReadLines("/proc/stat").First();
        // Format: cpu  user nice system idle iowait irq softirq steal guest guest_nice
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var user = long.Parse(parts[1]);
        var nice = long.Parse(parts[2]);
        var system = long.Parse(parts[3]);
        var idle = long.Parse(parts[4]);
        var iowait = long.Parse(parts[5]);
        var irq = long.Parse(parts[6]);
        var softirq = long.Parse(parts[7]);
        var steal = long.Parse(parts[8]);

        var totalIdle = idle + iowait;
        var total = user + nice + system + totalIdle + irq + softirq + steal;

        var deltaIdle = totalIdle - _prevIdle;
        var deltaTotal = total - _prevTotal;

        _prevIdle = totalIdle;
        _prevTotal = total;

        if (deltaTotal == 0)
            return 0;

        return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
    }

    /// <summary>
    /// Returns CPU temperature in degrees Celsius, or null if not available.
    /// Reads from the k10temp (AMD) or coretemp (Intel) hwmon sensor.
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

    private static string? GetTempPath()
    {
        if (_tempProbed) return _tempPath;
        _tempProbed = true;

        const string hwmonDir = "/sys/class/hwmon";
        if (!Directory.Exists(hwmonDir)) return null;

        // k10temp = AMD, coretemp = Intel
        string[] knownSensors = ["k10temp", "coretemp"];

        foreach (var hwmon in Directory.GetDirectories(hwmonDir))
        {
            var nameFile = Path.Combine(hwmon, "name");
            if (!File.Exists(nameFile)) continue;

            var name = File.ReadAllText(nameFile).Trim();
            if (!knownSensors.Contains(name)) continue;

            var tempFile = Path.Combine(hwmon, "temp1_input");
            if (!File.Exists(tempFile)) continue;
            _tempPath = tempFile;
            return _tempPath;
        }

        return null;
    }
}
