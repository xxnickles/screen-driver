using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record DiskMeter(int DriveIndex = 0) : PercentMeter
{
    public override string Label => "Disk";
    public override string MaxText => "9999GB / 9999GB";

    public override double Percent
    {
        get
        {
            var disk = GetDisk();
            return disk is null ? 0 : (double)disk.UsedBytes / disk.TotalBytes * 100.0;
        }
    }

    public override string Format()
    {
        var disk = GetDisk();
        if (disk is null) return "---";

        var usedGb = disk.UsedBytes / (1024.0 * 1024 * 1024);
        var totalGb = disk.TotalBytes / (1024.0 * 1024 * 1024);
        return $"{usedGb:F0}GB / {totalGb:F0}GB";
    }

    private DiskInfo? GetDisk()
    {
        var disks = DiskStats.GetUsage();
        return DriveIndex < disks.Count ? disks[DriveIndex] : null;
    }
}
