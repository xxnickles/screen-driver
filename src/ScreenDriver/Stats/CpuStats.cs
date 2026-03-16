namespace ScreenDriver.Stats;

/// <summary>
/// Reads CPU usage from /proc/stat using delta-based jiffy comparison.
/// </summary>
public static class CpuStats
{
    private static long _prevIdle;
    private static long _prevTotal;

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
}
