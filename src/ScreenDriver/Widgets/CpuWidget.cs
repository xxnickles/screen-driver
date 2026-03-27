using SkiaSharp;

namespace ScreenDriver.Widgets;

public record CpuUsageWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    // Delta state
    private long _prevIdle;
    private long _prevTotal;

    public CpuUsageWidget(
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

        var usage = ReadUsagePercent();
        var text = $"{usage:F0}%";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    private double ReadUsagePercent()
    {
        var line = File.ReadLines("/proc/stat").First();
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var user = long.Parse(parts[1]);
        var nice = long.Parse(parts[2]);
        var system = long.Parse(parts[3]);
        var idle = long.Parse(parts[4]);
        var iowait = long.Parse(parts[5]);
        var irq = long.Parse(parts[6]);
        var softirq = long.Parse(parts[7]);
        var steal = long.Parse(parts[8]);

        var totalIdle = idle + iowait;
        var total = user + nice + system + totalIdle + irq + softirq + steal;

        var deltaIdle = totalIdle - _prevIdle;
        var deltaTotal = total - _prevTotal;

        _prevIdle = totalIdle;
        _prevTotal = total;

        if (deltaTotal == 0) return 0;
        return (1.0 - (double)deltaIdle / deltaTotal) * 100.0;
    }
}

public record CpuTempWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    // Probe state
    private string? _tempPath;
    private bool _tempProbed;

    public CpuTempWidget(
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

        var temp = ReadTemperatureCelsius();
        var text = temp is null ? "--\u00b0C" : $"{temp}\u00b0C";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    private int? ReadTemperatureCelsius()
    {
        var path = ProbeTempPath();
        if (path is null) return null;

        try
        {
            var text = File.ReadAllText(path).Trim();
            return int.Parse(text) / 1000;
        }
        catch
        {
            return null;
        }
    }

    private string? ProbeTempPath()
    {
        if (_tempProbed) return _tempPath;
        _tempProbed = true;

        const string hwmonDir = "/sys/class/hwmon";
        if (!Directory.Exists(hwmonDir)) return null;

        string[] knownSensors = ["k10temp", "coretemp"];

        foreach (var hwmon in Directory.GetDirectories(hwmonDir))
        {
            var nameFile = Path.Combine(hwmon, "name");
            if (!File.Exists(nameFile)) continue;

            var name = File.ReadAllText(nameFile).Trim();
            if (!knownSensors.Contains(name)) continue;

            var tempFile = Path.Combine(hwmon, "temp1_input");
            if (!File.Exists(tempFile)) continue;
            _tempPath = tempFile;
            return _tempPath;
        }

        return null;
    }
}
