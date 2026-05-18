using ScreenDriver.Rendering;
using SkiaSharp;

namespace ScreenDriver.Widgets;

public record GpuMemoryBarWidget : Widget
{
    private readonly SKColor _fillColor;
    private readonly SKColor? _borderColor;
    private readonly SKBitmap _backgroundSlice;

    public GpuMemoryBarWidget(
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

    private static string? _totalPath;
    private static string? _usedPath;
    private static bool _probed;

    internal static (double UsedPercent, long TotalMb, long UsedMb) ReadMemoryUsage()
    {
        ProbePaths();
        if (_totalPath is null || _usedPath is null)
            return (0, 0, 0);

        var totalBytes = long.Parse(File.ReadAllText(_totalPath).Trim());
        var usedBytes = long.Parse(File.ReadAllText(_usedPath).Trim());

        if (totalBytes == 0)
            return (0, 0, 0);

        var usedPercent = (double)usedBytes / totalBytes * 100.0;
        const long mb = 1024 * 1024;
        return (usedPercent, totalBytes / mb, usedBytes / mb);
    }

    private static void ProbePaths()
    {
        if (_probed) return;
        _probed = true;

        const string drmPath = "/sys/class/drm";
        if (!Directory.Exists(drmPath)) return;

        foreach (var cardDir in Directory.GetDirectories(drmPath, "card*"))
        {
            var total = Path.Combine(cardDir, "device", "mem_info_vram_total");
            var used = Path.Combine(cardDir, "device", "mem_info_vram_used");
            if (!File.Exists(total) || !File.Exists(used)) continue;
            _totalPath = total;
            _usedPath = used;
            break;
        }
    }
}

public record GpuMemoryTextWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    public GpuMemoryTextWidget(
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

        var (_, totalMb, usedMb) = GpuMemoryBarWidget.ReadMemoryUsage();
        var text = $"{usedMb} / {totalMb} MB";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}

public record GpuMemoryPercentWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    public GpuMemoryPercentWidget(
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

        var (usedPercent, _, _) = GpuMemoryBarWidget.ReadMemoryUsage();
        var text = $"{usedPercent:F0}%";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }
}

public record GpuMemoryGaugeWidget : Widget
{
    private readonly SKColor _fillColor;
    private readonly float _radius;
    private readonly float _strokeWidth;
    private readonly SKColor? _trackColor;
    private readonly GaugeLabel? _label;
    private readonly SKBitmap _backgroundSlice;

    public GpuMemoryGaugeWidget(
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

        var (usedPercent, _, _) = GpuMemoryBarWidget.ReadMemoryUsage();
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
