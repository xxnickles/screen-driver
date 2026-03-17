namespace ScreenDriver.Stats;

/// <summary>
/// Reads disk space usage from /proc/mounts, deduplicated by physical device.
/// Excludes removable media (/run/media/*).
/// Labels use filesystem labels from /dev/disk/by-label/ when available,
/// falling back to device type shorthand (NVMe, SATA 1, etc.).
/// </summary>
public static class DiskStats
{
    /// <summary>
    /// Returns space usage for each unique internal physical device.
    /// </summary>
    public static IReadOnlyList<DiskInfo> GetUsage()
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

            // Only real block devices
            if (!device.StartsWith("/dev/")) continue;

            // Exclude removable media
            if (mountPoint.StartsWith("/run/media/")) continue;

            // Deduplicate by device path
            if (!seen.Add(device)) continue;

            try
            {
                var info = new DriveInfo(mountPoint);
                if (!info.IsReady) continue;

                var label = ResolveLabel(device, labelMap, ref sataIndex);
                var totalGb = info.TotalSize / (1024.0 * 1024 * 1024);
                var usedGb = (info.TotalSize - info.AvailableFreeSpace) / (1024.0 * 1024 * 1024);

                results.Add(new DiskInfo(label, device, usedGb, totalGb));
            }
            catch
            {
                // Skip inaccessible mounts
            }
        }

        return results;
    }

    private static string ResolveLabel(string device, Dictionary<string, string> labelMap, ref int sataIndex)
    {
        // Try filesystem label first
        if (labelMap.TryGetValue(device, out var fsLabel))
            return fsLabel;

        // Fall back to device type shorthand
        var devName = Path.GetFileName(device);

        if (devName.StartsWith("nvme"))
            return "NVMe";

        sataIndex++;
        return sataIndex == 1 ? "SATA" : $"SATA {sataIndex}";
    }

    /// <summary>
    /// Builds a map from device path (e.g., /dev/nvme0n1p2) to filesystem label
    /// by reading /dev/disk/by-label/ symlinks.
    /// </summary>
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

                // Unescape label (e.g., \x20 → space)
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

public record DiskInfo(string Label, string Device, double UsedGb, double TotalGb);
