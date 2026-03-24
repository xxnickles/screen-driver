using ScreenDriver.Meters;
using SkiaSharp;

namespace ScreenDriver.Widgets;

/// <summary>
/// Renders meter text into an auto-sized screen zone.
/// (X, Y) is the center anchor point. Zone is computed from MaxText font metrics.
/// Supports multiline text via \n in meter output.
/// Background is composited from the shared ScreenBackground.
/// </summary>
public record TextWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _size;
    private readonly SKColor _foreground;
    private readonly SKBitmap _backgroundSlice;

    public Meter Meter { get; }

    public TextWidget(
        int x,
        int y,
        Meter meter,
        ScreenBackground background,
        SKColor foreground,
        float size,
        TimeSpan interval,
        SKTypeface? typeface = null) : base(ComputeZone(x, y, meter.MaxText, size, typeface), interval)
    {
        Meter = meter;
        _foreground = foreground;
        _size = size;
        _typeface = typeface ?? SKTypeface.Default;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        using var font = new SKFont(_typeface, _size);
        using var paint = new SKPaint();
        paint.Color = _foreground;
        paint.IsAntialias = true;

        var text = Meter.Format();
        var lines = text.Split('\n');
        var metrics = font.Metrics;
        var lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
        var totalHeight = lineHeight * lines.Length;
        var startY = (Zone.Height - totalHeight) / 2f - metrics.Ascent;

        for (var i = 0; i < lines.Length; i++)
        {
            var drawY = startY + i * lineHeight;
            canvas.DrawText(lines[i], Zone.Width / 2f, drawY, SKTextAlign.Center, font, paint);
        }

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    private static WidgetZone ComputeZone(int centerX, int centerY, string maxText, float size, SKTypeface? typeface)
    {
        using var font = new SKFont(typeface ?? SKTypeface.Default, size);
        var metrics = font.Metrics;
        var lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

        var maxLines = maxText.Split('\n');
        var width = 0f;
        foreach (var line in maxLines)
        {
            var w = font.MeasureText(line);
            if (w > width) width = w;
        }

        // Add small padding (2px each side)
        var totalWidth = Math.Max((int)Math.Ceiling(width) + 4, 1);
        var totalHeight = Math.Max((int)Math.Ceiling(lineHeight * maxLines.Length) + 2, 1);

        var x = centerX - totalWidth / 2;
        var y = centerY - totalHeight / 2;

        return new WidgetZone(x, y, totalWidth, totalHeight);
    }
}
