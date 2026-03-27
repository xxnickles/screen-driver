using SkiaSharp;

namespace ScreenDriver.Widgets;

public enum NetworkDirection { Down, Up }

public record NetworkWidget : Widget
{
    private readonly SKTypeface _typeface;
    private readonly float _fontSize;
    private readonly SKColor _color;
    private readonly NetworkDirection _direction;
    private readonly SKBitmap _backgroundSlice;

    // Interface probe state
    private string? _activeInterface;
    private bool _probed;

    // Delta state
    private long _prevRxBytes;
    private long _prevTxBytes;
    private DateTime _prevTime;

    public NetworkWidget(
        WidgetZone zone,
        TimeSpan interval,
        ScreenBackground background,
        SKTypeface typeface,
        float fontSize,
        SKColor color,
        NetworkDirection direction) : base(zone, interval)
    {
        _typeface = typeface;
        _fontSize = fontSize;
        _color = color;
        _direction = direction;
        _backgroundSlice = background.CropZone(Zone);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var (downSpeed, upSpeed) = ReadSpeed();
        var speed = _direction == NetworkDirection.Down ? downSpeed : upSpeed;
        var text = FormatSpeed(speed);

        var y = RenderHelpers.GetAscent(_typeface, _fontSize);
        RenderHelpers.DrawText(canvas, text, _typeface, _fontSize, _color,
            Zone.Width / 2f, y);

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    private (double Down, double Up) ReadSpeed()
    {
        var iface = ProbeActiveInterface();
        if (iface is null) return (0, 0);

        var (rxBytes, txBytes) = ReadInterfaceBytes(iface);
        var now = DateTime.UtcNow;

        if (_prevTime == default)
        {
            _prevRxBytes = rxBytes;
            _prevTxBytes = txBytes;
            _prevTime = now;
            return (0, 0);
        }

        var elapsed = (now - _prevTime).TotalSeconds;
        if (elapsed <= 0) return (0, 0);

        var downSpeed = Math.Max((rxBytes - _prevRxBytes) / elapsed, 0);
        var upSpeed = Math.Max((txBytes - _prevTxBytes) / elapsed, 0);

        _prevRxBytes = rxBytes;
        _prevTxBytes = txBytes;
        _prevTime = now;

        return (downSpeed, upSpeed);
    }

    private static (long rxBytes, long txBytes) ReadInterfaceBytes(string iface)
    {
        foreach (var line in File.ReadLines("/proc/net/dev"))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(iface + ":")) continue;

            var parts = trimmed[(iface.Length + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var rxBytes = long.Parse(parts[0]);
            var txBytes = long.Parse(parts[8]);
            return (rxBytes, txBytes);
        }

        return (0, 0);
    }

    private string? ProbeActiveInterface()
    {
        if (_probed) return _activeInterface;
        _probed = true;

        try
        {
            foreach (var line in File.ReadLines("/proc/net/route"))
            {
                var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                if (parts[1] == "00000000")
                {
                    _activeInterface = parts[0];
                    return _activeInterface;
                }
            }
        }
        catch
        {
            // Best effort
        }

        return null;
    }

    private static string FormatSpeed(double bytesPerSec)
    {
        return bytesPerSec switch
        {
            >= 1_000_000_000 => $"{bytesPerSec / 1_000_000_000:F1} GB/s",
            >= 1_000_000 => $"{bytesPerSec / 1_000_000:F1} MB/s",
            >= 1_000 => $"{bytesPerSec / 1_000:F0} KB/s",
            _ => $"{bytesPerSec:F0} B/s"
        };
    }
}
