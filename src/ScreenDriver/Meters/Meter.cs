namespace ScreenDriver.Meters;

public abstract record Meter
{
    public abstract string Label { get; }
    public abstract string MaxText { get; }
    public abstract string Format();
}

public abstract record PercentMeter : Meter
{
    public abstract double Percent { get; }
}
