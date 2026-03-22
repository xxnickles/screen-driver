using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record MemoryMeter : PercentMeter
{
    public override string Label => "RAM";
    public override string MaxText => "99999MB / 99999MB";

    public override double Percent => MemoryStats.GetUsagePercent().UsedPercent;

    public override string Format()
    {
        var (_, totalMb, usedMb) = MemoryStats.GetUsagePercent();
        return $"{usedMb}MB / {totalMb}MB";
    }
}
