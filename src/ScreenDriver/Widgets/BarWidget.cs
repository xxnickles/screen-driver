using ScreenDriver.Meters;
using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Renders a horizontal fill bar proportional to a PercentMeter value.
/// Background is composited from the shared ScreenBackground.
/// No embedded label — pair with a separate TextWidget if needed.
/// </summary>
public record BarWidget : Widget
{
    private readonly SKColor _fill;
    private readonly SKColor? _border;
    private readonly SKBitmap _backgroundSlice;

    public PercentMeter Meter { get; }

    public BarWidget(
        int x,
        int y,
        int width,
        int height,
        PercentMeter meter,
        ScreenBackground background,
        SKColor fill,
        TimeSpan interval,
        SKColor? border = null) : base(new WidgetZone(x, y, width, height), interval)
    {
        Meter = meter;
        _fill = fill;
        _border = border;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var value = Math.Clamp(Meter.Percent, 0, 100);
        var fillWidth = (float)(Zone.Width * value / 100.0);

        using var fillPaint = new SKPaint();
        fillPaint.Color = _fill;
        canvas.DrawRect(0, 0, fillWidth, Zone.Height, fillPaint);

        if (_border is not { } borderColor)
            return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
        
        using var borderPaint = new SKPaint();
        borderPaint.Color = borderColor;
        borderPaint.Style = SKPaintStyle.Stroke;
        borderPaint.StrokeWidth = 1;
        borderPaint.IsAntialias = false;
        canvas.DrawRect(0, 0, Zone.Width - 1, Zone.Height - 1, borderPaint);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
