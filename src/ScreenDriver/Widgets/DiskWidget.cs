using SkiaSharp;

namespace ScreenDriver.Widgets;

public record DiskWidget : Widget
{
    private const long MinCapacityBytes = 64L * 1024 * 1024 * 1024; // 64 GB

    private readonly SKTypeface _typeface;
    private readonly float _labelFontSize;
    private readonly float _valueFontSize;
    private readonly SKColor _labelColor;
    private readonly SKColor _valueColor;
    private readonly int _maxDrives;
    private readonly SKBitmap _backgroundSlice;
    private readonly float _labelLineHeight;
    private readonly float _valueLineHeight;

    public DiskWidget(
        WidgetZone zone,
        TimeSpan interval,
        ScreenBackground background,
        SKTypeface typeface,
        float labelFontSize,
        float valueFontSize,
        SKColor labelColor,
        SKColor valueColor,
        int maxDrives = 3) : base(zone, interval)
    {
        _typeface = typeface;
        _labelFontSize = labelFontSize;
        _valueFontSize = valueFontSize;
        _labelColor = labelColor;
        _valueColor = valueColor;
        _maxDrives = maxDrives;
        _backgroundSlice = background.CropZone(Zone);

        RenderHelpers.MeasureText("A", typeface, labelFontSize, out _labelLineHeight);
        RenderHelpers.MeasureText("0", typeface, valueFontSize, out _valueLineHeight);
    }

    public override Rgb565Frame Render()
    {
        using var bitmap = _backgroundSlice.Copy();
        using var canvas = new SKCanvas(bitmap);

        var disks = ReadDiskUsage();
        if (disks.Count == 0)
        {
            RenderHelpers.DrawText(canvas, "---", _typeface, _valueFontSize, _valueColor,
                Zone.Width / 2f, RenderHelpers.GetAscent(_typeface, _valueFontSize));
            return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
        }

        var y = 0f;
        foreach (var disk in disks)
        {
            // Label line
            var labelY = y + RenderHelpers.GetAscent(_typeface, _labelFontSize);
            RenderHelpers.DrawText(canvas, disk.Label, _typeface, _labelFontSize, _labelColor,
                Zone.Width / 2f, labelY);
            y += _labelLineHeight;

            // Value line
            var usedGb = disk.UsedBytes / (1024.0 * 1024 * 1024);
            var totalGb = disk.TotalBytes / (1024.0 * 1024 * 1024);
            var valueText = $"{usedGb:F0} / {totalGb:F0} GB";

            var valueY = y + RenderHelpers.GetAscent(_typeface, _valueFontSize);
            RenderHelpers.DrawText(canvas, valueText, _typeface, _valueFontSize, _valueColor,
                Zone.Width / 2f, valueY);
            y += _valueLineHeight;

            // Spacing between drives
            y += _valueLineHeight * 0.3f;
        }

        return Rgb565Frame.FromBgra8888(bitmap.GetPixelSpan(), Zone.Width, Zone.Height);
    }

    private IReadOnlyList<DiskInfo> ReadDiskUsage()
    {
        var labelMap = BuildLabelMap();
        var seen = new HashSet<string>();
        var results = new List<DiskInfo>();
        var sataIndex = 0;

        foreach (var line in File.ReadLines("/proc/mounts"))
        {
            var parts = line.Split(' ');
            if (parts.Length < 3) continue;

            var device = parts[0];
            var mountPoint = parts[1];

            if (!device.StartsWith("/dev/")) continue;
            if (!seen.Add(device)) continue;

            try
            {
                var info = new DriveInfo(mountPoint);
                if (!info.IsReady) continue;
                if (info.TotalSize < MinCapacityBytes) continue;

                var label = ResolveLabel(device, labelMap, ref sataIndex);
                results.Add(new DiskInfo(label, device, info.TotalSize - info.AvailableFreeSpace, info.TotalSize));
            }
            catch
            {
                // Skip inaccessible mounts
            }
        }

        return results
            .OrderByDescending(d => d.TotalBytes)
            .Take(_maxDrives)
            .ToList();
    }

    private static string ResolveLabel(string device, Dictionary<string, string> labelMap, ref int sataIndex)
    {
        if (labelMap.TryGetValue(device, out var fsLabel))
            return fsLabel;

        var devName = Path.GetFileName(device);

        if (devName.StartsWith("nvme"))
            return "NVMe";

        sataIndex++;
        return sataIndex == 1 ? "SATA" : $"SATA {sataIndex}";
    }

    private static Dictionary<string, string> BuildLabelMap()
    {
        var map = new Dictionary<string, string>();
        const string labelDir = "/dev/disk/by-label";

        if (!Directory.Exists(labelDir)) return map;

        try
        {
            foreach (var link in Directory.GetFiles(labelDir))
            {
                var target = Path.GetFullPath(
                    Path.Combine(labelDir, File.ResolveLinkTarget(link, false)?.FullName ?? ""));

                var label = Path.GetFileName(link).Replace("\\x20", " ");
                map[target] = label;
            }
        }
        catch
        {
            // Best effort
        }

        return map;
    }

    public static WidgetZone ComputeZone(
        int centerX, int y, SKTypeface typeface,
        float labelFontSize, float valueFontSize, string maxValueText, int maxDrives)
    {
        RenderHelpers.MeasureText("A", typeface, labelFontSize, out var labelLineHeight);
        var valueWidth = RenderHelpers.MeasureText(maxValueText, typeface, valueFontSize, out var valueLineHeight);
        var labelWidth = RenderHelpers.MeasureText("SATA 2", typeface, labelFontSize, out _);

        var driveHeight = labelLineHeight + valueLineHeight;
        var spacing = valueLineHeight * 0.3f;
        var totalHeight = driveHeight * maxDrives + spacing * (maxDrives - 1);

        var width = Math.Max(labelWidth, valueWidth);
        var totalWidth = Math.Max((int)Math.Ceiling(width) + 4, 1);
        var x = centerX - totalWidth / 2;

        return new WidgetZone(x, y, totalWidth, Math.Max((int)Math.Ceiling(totalHeight) + 2, 1));
    }

    private record DiskInfo(string Label, string Device, long UsedBytes, long TotalBytes);
}
