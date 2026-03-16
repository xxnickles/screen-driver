namespace ScreenDriver.Widgets;

/// <summary>
/// Defines a rectangular region on the screen where a widget renders.
/// </summary>
public readonly record struct WidgetZone(int X, int Y, int Width, int Height)
{
    public int EndX => X + Width - 1;
    public int EndY => Y + Height - 1;
}
