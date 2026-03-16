using ScreenDriver.Widgets;

namespace ScreenDriver.Queue;

/// <summary>
/// A request to write a rendered frame to a screen region.
/// </summary>
public record WriteRequest(WidgetZone Zone, Rgb565Frame Frame);
