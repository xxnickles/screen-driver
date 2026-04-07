using ScreenDriver.Rendering;
using SkiaSharp;

namespace ScreenDriver.Widgets;

public record MemoryBarWidget : Widget
{
    private readonly SKColor _fillColor;
    private readonly SKColor? _borderColor;
    private readonly SKBitmap _backgroundSlice;

    public MemoryBarWidget(
        WidgetZone zone,
        TimeSpan interval,
        ScreenBackground background,
        SKColor fillColor,
        SKColor? borderColor = null) : base(zone, interval)
    {
        _fillColor = fillColor;
        _borderColor = borderColor;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var (usedPercent, _, _) = ReadMemoryUsage();
        RenderHelpers.DrawBar(canvas, usedPercent, _fillColor, Zone.Width, Zone.Height, _borderColor);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    internal static (double UsedPercent, long TotalMb, long UsedMb) ReadMemoryUsage()
    {
        long totalKb = 0;
        long availableKb = 0;

        foreach (var line in File.ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:"))
                totalKb = ParseKb(line);
            else if (line.StartsWith("MemAvailable:"))
                availableKb = ParseKb(line);

            if (totalKb > 0 && availableKb > 0)
                break;
        }

        if (totalKb == 0)
            return (0, 0, 0);

        var usedKb = totalKb - availableKb;
        var percent = (double)usedKb / totalKb * 100.0;

        return (percent, totalKb / 1024, usedKb / 1024);
    }

    private static long ParseKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.Parse(parts[1]);
    }
}

public record MemoryTextWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    public MemoryTextWidget(
        WidgetZone zone,
        TimeSpan interval,
        ScreenBackground background,
        SKTypeface typeface,
        float fontSize,
        SKColor color) : base(zone, interval)
    {
        _typeface = typeface;
        _fontSize = fontSize;
        _color = color;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var (_, totalMb, usedMb) = MemoryBarWidget.ReadMemoryUsage();
        var text = $"{usedMb} / {totalMb} MB";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}

public record MemoryPercentWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    public MemoryPercentWidget(
        WidgetZone zone,
        TimeSpan interval,
        ScreenBackground background,
        SKTypeface typeface,
        float fontSize,
        SKColor color) : base(zone, interval)
    {
        _typeface = typeface;
        _fontSize = fontSize;
        _color = color;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var (usedPercent, _, _) = MemoryBarWidget.ReadMemoryUsage();
        var text = $"{usedPercent:F0}%";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}

public record GaugeLabel(SKTypeface Typeface, float FontSize, SKColor Color);

public record MemoryGaugeWidget : Widget
{
    private readonly SKColor _fillColor;
    private readonly float _radius;
    private readonly float _strokeWidth;
    private readonly SKColor? _trackColor;
    private readonly GaugeLabel? _label;
    private readonly SKBitmap _backgroundSlice;

    public MemoryGaugeWidget(
        WidgetZone zone,
        TimeSpan interval,
        ScreenBackground background,
        SKColor fillColor,
        float radius,
        float strokeWidth,
        SKColor? trackColor = null,
        GaugeLabel? label = null) : base(zone, interval)
    {
        _fillColor = fillColor;
        _radius = radius;
        _strokeWidth = strokeWidth;
        _trackColor = trackColor;
        _label = label;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var (usedPercent, _, _) = MemoryBarWidget.ReadMemoryUsage();
        RenderHelpers.DrawRadialGauge(canvas, usedPercent, _fillColor,
            Zone.Width, Zone.Height, _radius, _strokeWidth, _trackColor);

        if (_label is { } lbl)
        {
            var text = $"{usedPercent:F0}%";
            using var font = new SKFont(lbl.Typeface, lbl.FontSize);
            var metrics = font.Metrics;
            var y = (Zone.Height - metrics.Ascent - metrics.Descent) / 2f;
            RenderHelpers.DrawText(canvas, text, lbl.Typeface, lbl.FontSize, lbl.Color,
                Zone.Width / 2f, y);
        }

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}
