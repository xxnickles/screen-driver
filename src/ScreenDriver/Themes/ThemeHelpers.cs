using ScreenDriver.Widgets;
using SkiaSharp;

namespace ScreenDriver.Themes;

public static class ThemeHelpers
{
    public static readonly string ThemesRoot = Path.Combine(AppContext.BaseDirectory, "themes");
    public static readonly string[] ImageExtensions = [".png", ".jpg", ".bmp"];
    public static readonly string[] FontExtensions = [".ttf", ".otf"];

    public static WidgetZone ComputeZone(int centerX, int centerY, string maxText, float size, SKTypeface typeface)
    {
        using var font = new SKFont(typeface, size);
        var metrics = font.Metrics;
        var lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

        var maxLines = maxText.Split('\n');
        var width = 0f;
        foreach (var line in maxLines)
        {
            var w = font.MeasureText(line);
            if (w > width) width = w;
        }

        var totalWidth = Math.Max((int)Math.Ceiling(width) + 4, 1);
        var totalHeight = Math.Max((int)Math.Ceiling(lineHeight * maxLines.Length) + 2, 1);
        var x = centerX - totalWidth / 2;

        return new WidgetZone(x, centerY, totalWidth, totalHeight);
    }

    public static string DiscoverSingle(string dir, string[] extensions)
    {
        var files = DiscoverAll(dir, extensions);
        return files.Length switch
        {
            0 => throw new InvalidOperationException($"No matching file found in: {dir}"),
            1 => files[0],
            _ => throw new InvalidOperationException($"Multiple matching files found in: {dir}")
        };
    }

    public static string[] DiscoverAll(string dir, string[] extensions)
    {
        return Directory.GetFiles(dir)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();
    }
}
