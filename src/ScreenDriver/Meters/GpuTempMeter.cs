using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record GpuTempMeter : Meter
{
    public override string Label => "GPU Temp";
    public override string MaxText => "999°C";

    public override string Format()
    {
        var temp = GpuStats.GetTemperatureCelsius();
        return temp is null ? "---" : $"{temp}°C";
    }
}
