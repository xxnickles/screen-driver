using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record GpuUsageMeter : PercentMeter
{
    public override string Label => "GPU";
    public override string MaxText => "100%";

    public override double Percent => GpuStats.GetUsagePercent() ?? 0;

    public override string Format()
    {
        var usage = GpuStats.GetUsagePercent();
        return usage is null ? "---" : $"{usage}%";
    }
}
