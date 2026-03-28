namespace ScreenDriver.Widgets;

/// <summary>
/// A renderable screen region that produces RGB565 frames on demand.
/// </summary>
public abstract record Widget(WidgetZone Zone, TimeSpan Interval)
{
    public Action<string>? EventRaised { get; set; }

    public abstract Rgb565Frame Render();
}
