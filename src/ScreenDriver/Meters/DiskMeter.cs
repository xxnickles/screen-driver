using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record DiskMeter(int MaxDrives = 3) : Meter
{
    public override string Label => "Drives";
    public override string MaxText => "SATA 2\n9999 / 9999 GB\nSATA 2\n9999 / 9999 GB\nSATA 2\n9999 / 9999 GB";

    public override string Format()
    {
        var disks = DiskStats.GetUsage(MaxDrives);
        if (disks.Count == 0) return "---";

        var lines = new string[disks.Count];
        for (var i = 0; i < disks.Count; i++)
        {
            var usedGb = disks[i].UsedBytes / (1024.0 * 1024 * 1024);
            var totalGb = disks[i].TotalBytes / (1024.0 * 1024 * 1024);
            lines[i] = $"{disks[i].Label}\n{usedGb:F0} / {totalGb:F0} GB";
        }

        return string.Join("\n\n", lines);
    }
}
