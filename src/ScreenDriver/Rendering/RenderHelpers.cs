using SkiaSharp;

namespace ScreenDriver.Rendering;

public static class RenderHelpers
{
    public static void DrawText(
        SKCanvas canvas,
        string text,
        SKTypeface typeface,
        float size,
        SKColor color,
        float x,
        float y,
        SKTextAlign align = SKTextAlign.Center)
    {
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint();
        paint.Color = color;
        paint.IsAntialias = true;
        canvas.DrawText(text, x, y, align, font, paint);
    }

    public static void DrawBar(
        SKCanvas canvas,
        double percent,
        SKColor fillColor,
        float width,
        float height,
        SKColor? borderColor = null)
    {
        var value = Math.Clamp(percent, 0, 100);
        var fillWidth = (float)(width * value / 100.0);

        using var fillPaint = new SKPaint();
        fillPaint.Color = fillColor;
        canvas.DrawRect(0, 0, fillWidth, height, fillPaint);

        if (borderColor is not { } border) return;

        using var borderPaint = new SKPaint();
        borderPaint.Color = border;
        borderPaint.Style = SKPaintStyle.Stroke;
        borderPaint.StrokeWidth = 1;
        borderPaint.IsAntialias = false;
        canvas.DrawRect(0, 0, width - 1, height - 1, borderPaint);
    }

    public static void DrawRadialGauge(
        SKCanvas canvas,
        double percent,
        SKColor fillColor,
        float width,
        float height,
        float radius,
        float strokeWidth,
        SKColor? trackColor = null)
    {
        var value = Math.Clamp(percent, 0, 100);
        var cx = width / 2f;
        var cy = height / 2f;
        var rect = new SKRect(cx - radius, cy - radius, cx + radius, cy + radius);

        if (trackColor is { } track)
        {
            using var trackPaint = new SKPaint();
            trackPaint.Color = track;
            trackPaint.Style = SKPaintStyle.Stroke;
            trackPaint.StrokeWidth = strokeWidth;
            trackPaint.StrokeCap = SKStrokeCap.Round;
            trackPaint.IsAntialias = true;
            canvas.DrawArc(rect, 0, 360, false, trackPaint);
        }

        var sweepAngle = (float)(value / 100.0 * 360.0);

        using var fillPaint = new SKPaint();
        fillPaint.Color = fillColor;
        fillPaint.Style = SKPaintStyle.Stroke;
        fillPaint.StrokeWidth = strokeWidth;
        fillPaint.StrokeCap = SKStrokeCap.Round;
        fillPaint.IsAntialias = true;
        canvas.DrawArc(rect, -90, sweepAngle, false, fillPaint);
    }

    public static float MeasureText(string text, SKTypeface typeface, float size, out float lineHeight)
    {
        using var font = new SKFont(typeface, size);
        var metrics = font.Metrics;
        lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;
        return font.MeasureText(text);
    }

    public static float GetAscent(SKTypeface typeface, float size)
    {
        using var font = new SKFont(typeface, size);
        return -font.Metrics.Ascent;
    }
}
