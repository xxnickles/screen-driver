using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record MemoryMeter : PercentMeter
{
    public override string Label => "MEM";
    public override string MaxText => "99999 / 99999 MB";

    public override double Percent => MemoryStats.GetUsagePercent().UsedPercent;

    public override string Format()
    {
        var (_, totalMb, usedMb) = MemoryStats.GetUsagePercent();
        return $"{usedMb} / {totalMb} MB";
    }
}
