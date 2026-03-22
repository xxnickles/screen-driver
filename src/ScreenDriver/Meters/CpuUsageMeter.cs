using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record CpuUsageMeter : PercentMeter
{
    public override string Label => "CPU";
    public override string MaxText => "100%";

    public override double Percent => CpuStats.GetUsagePercent();

    public override string Format() => $"{CpuStats.GetUsagePercent():F0}%";
}
