using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record NetworkUpMeter : Meter
{
    public override string Label => "UP";
    public override string MaxText => "999.9 MB/s";

    public override string Format()
    {
        var (_, up) = NetworkStats.GetSpeed();
        return NetworkStats.FormatSpeed(up);
    }
}
