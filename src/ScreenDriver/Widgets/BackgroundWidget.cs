namespace ScreenDriver.Widgets;

/// <summary>
/// Full-screen background widget. Converts the ScreenBackground bitmap to RGB565 at construction.
/// Renders once (infinite interval) — re-sent on reconnect when the scheduler restarts.
/// </summary>
public record BackgroundWidget : Widget
{
    private readonly Rgb565Frame _frame;

    public BackgroundWidget(ScreenBackground background)
        : base(new WidgetZone(0, 0, background.Bitmap.Width, background.Bitmap.Height), Timeout.InfiniteTimeSpan)
    {
        _frame = Rgb565Frame.FromBgra8888(
            background.Bitmap.GetPixelSpan(),
            background.Bitmap.Width,
            background.Bitmap.Height);
    }

    public override Rgb565Frame Render() => _frame;
}
