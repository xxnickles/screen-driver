using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Renders a horizontal bar proportional to a 0–100 value with a percentage label.
/// </summary>
public record BarWidget(
    WidgetZone Zone,
    TimeSpan Interval,
    Func<double> GetValue,
    SKColor Background,
    SKColor FillColor,
    SKColor LabelColor,
    float TextSize) : Widget(Zone, Interval)
{
    public override Rgb565Frame Render()
    {
        using var bitmap = new SKBitmap(Zone.Width, Zone.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(Background);

        var value = Math.Clamp(GetValue(), 0, 100);
        var fillWidth = (float)(Zone.Width * value / 100.0);

        using var fillPaint = new SKPaint();
        fillPaint.Color = FillColor;
        canvas.DrawRect(0, 0, fillWidth, Zone.Height, fillPaint);

        using var font = new SKFont(SKTypeface.Default, TextSize);
        using var labelPaint = new SKPaint();
        labelPaint.Color = LabelColor;
        labelPaint.IsAntialias = true;

        var label = $"{value:F0}%";
        var labelWidth = font.MeasureText(label);
        var metrics = font.Metrics;
        var x = (Zone.Width - labelWidth) / 2f;
        var y = (Zone.Height - metrics.Ascent - metrics.Descent) / 2f - metrics.Ascent;

        canvas.DrawText(label, x, y, SKTextAlign.Left, font, labelPaint);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
