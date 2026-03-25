using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record NetworkDownMeter : Meter
{
    public override string Label => "DOWN";
    public override string MaxText => "999.9 MB/s";

    public override string Format()
    {
        var (down, _) = NetworkStats.GetSpeed();
        return NetworkStats.FormatSpeed(down);
    }
}
