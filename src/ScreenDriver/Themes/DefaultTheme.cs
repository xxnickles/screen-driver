using ScreenDriver.Widgets;
using SkiaSharp;
using static ScreenDriver.Themes.ThemeHelpers;

namespace ScreenDriver.Themes;

public record DefaultTheme : Theme
{
    private const string Name = "default";

    public DefaultTheme()
        : base(BuildWidgets()) { }

    private static Widget[] BuildWidgets()
    {
        var dir = Path.Combine(ThemesRoot, Name);
        var imagePath = DiscoverSingle(dir, ImageExtensions);
        var fontPaths = DiscoverAll(dir, FontExtensions);
        var typeface = fontPaths.Length == 1 ? SKTypeface.FromFile(fontPaths[0]) : SKTypeface.Default;
        var background = ScreenBackground.FromImage(imagePath, ScreenDevice.ScreenWidth, ScreenDevice.ScreenHeight);

        return
        [
            new BackgroundWidget(background),

            // CPU panel (top-left)
            new CpuUsageWidget(
                ComputeZone(75, 80, "100%", 34f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 34f, SKColors.White),

            new CpuTempWidget(
                ComputeZone(75, 130, "999\u00b0C", 18f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 18f, SKColors.White),

            // GPU panel (top-right)
            new GpuUsageWidget(
                ComputeZone(240, 80, "100%", 34f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 34f, SKColors.White),

            new GpuTempWidget(
                ComputeZone(240, 130, "999\u00b0C", 18f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 18f, SKColors.White),

            // Drives panel (right side)
            new DiskWidget(
                DiskWidget.ComputeZone(400, 20, typeface, 13f, 13f, "9999 / 9999 GB", 3),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: 16f, valueFontSize: 13f,
                labelColor: SKColors.Crimson, valueColor: SKColors.DodgerBlue),

            // Memory panel (middle, wide)
            new MemoryBarWidget(
                new WidgetZone(115, 195, 140, 15),
                TimeSpan.FromSeconds(5),
                background,
                SKColors.DodgerBlue, SKColors.Azure),

            new MemoryTextWidget(
                ComputeZone(190, 220, "99999 / 99999 MB", 17f, typeface),
                TimeSpan.FromSeconds(5),
                background, typeface, 17f, SKColors.White),

            // Network (bottom strip)
            new NetworkWidget(
                ComputeZone(110, 280, "999.9 MB/s", 13f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 13f, SKColors.White,
                NetworkDirection.Down),

            new NetworkWidget(
                ComputeZone(240, 280, "999.9 MB/s", 13f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 13f, SKColors.White,
                NetworkDirection.Up),
        ];
    }
}
