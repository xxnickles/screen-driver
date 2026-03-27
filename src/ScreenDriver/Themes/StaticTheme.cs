using ScreenDriver.Widgets;
using SkiaSharp;
using static ScreenDriver.Themes.ThemeHelpers;

namespace ScreenDriver.Themes;

public record StaticTheme : Theme
{
    private const string Name = "static";

    public StaticTheme()
        : base(BuildWidgets())
    {
    }

    private static Widget[] BuildWidgets()
    {
        var dir = Path.Combine(ThemesRoot, Name);
        var fontPaths = DiscoverAll(dir, FontExtensions);
        var typeface = fontPaths.Length == 1 ? SKTypeface.FromFile(fontPaths[0]) : SKTypeface.Default;
        var background =
            ScreenBackground.SolidColor(SKColors.Crimson, ScreenDevice.ScreenWidth, ScreenDevice.ScreenHeight);

        return
        [
            new BackgroundWidget(background),

            // CPU panel (top-left)
            new TextWidget(
                ComputeZone(75, 40, "CPU", 24f, typeface),
                background, typeface, 24f, SKColors.Black, "CPU"),
            new CpuUsageWidget(
                ComputeZone(75, 70, "100%", 38f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 34f, SKColors.Black),

            new CpuTempWidget(
                ComputeZone(75, 120, "999\u00b0C", 24f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 18f, SKColors.Black),

            // GPU panel (top-right)
            new TextWidget(
                ComputeZone(240, 40, "GPU", 24f, typeface),
                background, typeface, 24f, SKColors.Black, "GPU"),
            
            new GpuUsageWidget(
                ComputeZone(240, 70, "100%", 38f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 34f, SKColors.Black),

            new GpuTempWidget(
                ComputeZone(240, 120, "999\u00b0C", 18f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 24f, SKColors.Black),

            // Drives panel (right side)
            new DiskWidget(
                DiskWidget.ComputeZone(400, 20, typeface, 16f, 14f, "99999 / 99999 GB", 3),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: 16f, valueFontSize: 14f,
                labelColor: SKColors.WhiteSmoke, valueColor: SKColors.Black),

            // Memory panel (middle, wide)
            new TextWidget(
                ComputeZone(30, 170, "RAM", 24f, typeface),
                background, typeface, 24f, SKColors.Black, "RAM"),

            new MemoryBarWidget(
                new WidgetZone(30, 215, 140, 15),
                TimeSpan.FromSeconds(5),
                background,
                SKColors.DodgerBlue, SKColors.Azure),

            new MemoryTextWidget(
                ComputeZone(100, 240, "99999 / 99999 MB", 17f, typeface),
                TimeSpan.FromSeconds(5),
                background, typeface, 17f, SKColors.Black),

            // Network (bottom strip)
            new NetworkWidget(
                ComputeZone(110, 280, "999.9 MB/s", 13f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 13f, SKColors.Black,
                NetworkDirection.Down),

            new NetworkWidget(
                ComputeZone(240, 280, "999.9 MB/s", 13f, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, 13f, SKColors.Black,
                NetworkDirection.Up),
        ];
    }
}