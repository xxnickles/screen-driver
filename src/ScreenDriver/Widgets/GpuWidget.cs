using SkiaSharp;

namespace ScreenDriver.Widgets;

public record GpuUsageWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    // Probe state
    private string? _usagePath;
    private bool _probed;

    public GpuUsageWidget(
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
        var text = usage is null ? "--%" : $"{usage}%";

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    private int? ReadUsagePercent()
    {
        ProbeUsagePath();
        if (_usagePath is null) return null;

        try
        {
            var text = File.ReadAllText(_usagePath).Trim();
            return int.Parse(text);
        }
        catch (Exception ex)
        {
            EventRaised?.Invoke($"Usage read failed: {ex.Message}");
            return null;
        }
    }

    private void ProbeUsagePath()
    {
        if (_probed) return;
        _probed = true;

        const string drmPath = "/sys/class/drm";
        if (!Directory.Exists(drmPath)) return;

        foreach (var cardDir in Directory.GetDirectories(drmPath, "card*"))
        {
            var usagePath = Path.Combine(cardDir, "device", "gpu_busy_percent");
            if (!File.Exists(usagePath)) continue;
            _usagePath = usagePath;
            break;
        }
    }
}

public record GpuTempWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly SKBitmap _backgroundSlice;

    // Probe state
    private string? _tempPath;
    private bool _probed;

    public GpuTempWidget(
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
        ProbeTempPath();
        if (_tempPath is null) return null;

        try
        {
            var text = File.ReadAllText(_tempPath).Trim();
            return int.Parse(text) / 1000;
        }
        catch (Exception ex)
        {
            EventRaised?.Invoke($"Temp read failed: {ex.Message}");
            return null;
        }
    }

    private void ProbeTempPath()
    {
        if (_probed) return;
        _probed = true;

        const string drmPath = "/sys/class/drm";
        if (!Directory.Exists(drmPath)) return;

        foreach (var cardDir in Directory.GetDirectories(drmPath, "card*"))
        {
            var hwmonDir = Path.Combine(cardDir, "device", "hwmon");
            if (!Directory.Exists(hwmonDir)) continue;

            foreach (var hwmon in Directory.GetDirectories(hwmonDir))
            {
                var nameFile = Path.Combine(hwmon, "name");
                if (!File.Exists(nameFile)) continue;

                var name = File.ReadAllText(nameFile).Trim();
                if (name != "amdgpu") continue;

                var tempFile = Path.Combine(hwmon, "temp1_input");
                if (File.Exists(tempFile))
                    _tempPath = tempFile;

                return;
            }
        }
    }
}
