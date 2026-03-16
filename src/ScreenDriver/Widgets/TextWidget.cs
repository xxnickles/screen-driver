using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Renders a text label into a screen zone.
/// </summary>
public record TextWidget(
    WidgetZone Zone,
    TimeSpan Interval,
    Func<string> GetText,
    SKColor Background,
    SKColor Foreground,
    float TextSize) : Widget(Zone, Interval)
{
    public override Rgb565Frame Render()
    {
        using var bitmap = new SKBitmap(Zone.Width, Zone.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(Background);

        using var font = new SKFont(SKTypeface.Default, TextSize);
        using var paint = new SKPaint();
        paint.Color = Foreground;
        paint.IsAntialias = true;

        var text = GetText();
        var metrics = font.Metrics;
        var y = (Zone.Height - metrics.Ascent - metrics.Descent) / 2f - metrics.Ascent;

        canvas.DrawText(text, 4f, y, SKTextAlign.Left, font, paint);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
