namespace ScreenDriver.Stats;

/// <summary>
/// Reads memory usage from /proc/meminfo.
/// </summary>
public static class MemoryStats
{
    /// <summary>
    /// Returns memory usage as (usedPercent, totalMb, usedMb).
    /// Uses MemAvailable for a realistic "used" figure (accounts for buffers/cache).
    /// </summary>
    public static (double UsedPercent, long TotalMb, long UsedMb) GetUsagePercent()
    {
        long totalKb = 0;
        long availableKb = 0;

        foreach (var line in File.ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:"))
                totalKb = ParseKb(line);
            else if (line.StartsWith("MemAvailable:"))
                availableKb = ParseKb(line);

            if (totalKb > 0 && availableKb > 0)
                break;
        }

        if (totalKb == 0)
            return (0, 0, 0);

        var usedKb = totalKb - availableKb;
        var percent = (double)usedKb / totalKb * 100.0;

        return (percent, totalKb / 1024, usedKb / 1024);
    }

    private static long ParseKb(string line)
    {
        // Format: "MemTotal:       65748832 kB"
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.Parse(parts[1]);
    }
}
