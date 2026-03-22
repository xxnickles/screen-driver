using ScreenDriver.Meters;
using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Renders a horizontal fill bar proportional to a PercentMeter value.
/// No embedded label — pair with a separate TextWidget if needed.
/// </summary>
public record BarWidget : Widget
{
    private readonly SKColor _background;
    private readonly SKColor _fill;

    public PercentMeter Meter { get; }

    public BarWidget(
        int x,
        int y,
        int width,
        int height,
        PercentMeter meter,
        SKColor background,
        SKColor fill,
        TimeSpan interval) : base(new WidgetZone(x, y, width, height), interval)
    {
        Meter = meter;
        _background = background;
        _fill = fill;
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = new SKBitmap(Zone.Width, Zone.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(_background);

        var value = Math.Clamp(Meter.Percent, 0, 100);
        var fillWidth = (float)(Zone.Width * value / 100.0);

        using var fillPaint = new SKPaint();
        fillPaint.Color = _fill;
        canvas.DrawRect(0, 0, fillWidth, Zone.Height, fillPaint);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
