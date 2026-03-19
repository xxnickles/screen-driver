namespace ScreenDriver.Stats;

/// <summary>
/// Reads disk space usage from /proc/mounts, deduplicated by physical device.
/// Filters by minimum capacity and limits results to a configurable count.
/// Labels use filesystem labels from /dev/disk/by-label/ when available,
/// falling back to device type shorthand (NVMe, SATA, etc.).
/// </summary>
public static class DiskStats
{
    private const long MinCapacityBytes = 64L * 1024 * 1024 * 1024; // 64 GB

    /// <summary>
    /// Returns space usage for the largest internal drives, up to maxDrives.
    /// </summary>
    public static IReadOnlyList<DiskInfo> GetUsage(int maxDrives = 3)
    {
        var labelMap = BuildLabelMap();
        var seen = new HashSet<string>();
        var results = new List<DiskInfo>();
        var sataIndex = 0;

        foreach (var line in File.ReadLines("/proc/mounts"))
        {
            var parts = line.Split(' ');
            if (parts.Length < 3) continue;

            var device = parts[0];
            var mountPoint = parts[1];

            if (!device.StartsWith("/dev/")) continue;
            if (!seen.Add(device)) continue;

            try
            {
                var info = new DriveInfo(mountPoint);
                if (!info.IsReady) continue;
                if (info.TotalSize < MinCapacityBytes) continue;

                var label = ResolveLabel(device, labelMap, ref sataIndex);

                results.Add(new DiskInfo(label, device, info.TotalSize - info.AvailableFreeSpace, info.TotalSize));
            }
            catch
            {
                // Skip inaccessible mounts
            }
        }

        return results
            .OrderByDescending(d => d.TotalBytes)
            .Take(maxDrives)
            .ToList();
    }

    private static string ResolveLabel(string device, Dictionary<string, string> labelMap, ref int sataIndex)
    {
        if (labelMap.TryGetValue(device, out var fsLabel))
            return fsLabel;

        var devName = Path.GetFileName(device);

        if (devName.StartsWith("nvme"))
            return "NVMe";

        sataIndex++;
        return sataIndex == 1 ? "SATA" : $"SATA {sataIndex}";
    }

    private static Dictionary<string, string> BuildLabelMap()
    {
        var map = new Dictionary<string, string>();
        const string labelDir = "/dev/disk/by-label";

        if (!Directory.Exists(labelDir)) return map;

        try
        {
            foreach (var link in Directory.GetFiles(labelDir))
            {
                var target = Path.GetFullPath(
                    Path.Combine(labelDir, File.ResolveLinkTarget(link, false)?.FullName ?? ""));

                var label = Path.GetFileName(link).Replace("\\x20", " ");
                map[target] = label;
            }
        }
        catch
        {
            // Best effort
        }

        return map;
    }
}

public record DiskInfo(string Label, string Device, long UsedBytes, long TotalBytes);
