using ScreenDriver.Stats;

namespace ScreenDriver.Meters;

public record CpuTempMeter : Meter
{
    public override string Label => "CPU Temp";
    public override string MaxText => "999°C";

    public override string Format()
    {
        var temp = CpuStats.GetTemperatureCelsius();
        return temp is null ? "---" : $"{temp}°C";
    }
}
