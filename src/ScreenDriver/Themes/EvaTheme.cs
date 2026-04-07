using ScreenDriver.Device;
using ScreenDriver.Rendering;
using ScreenDriver.Widgets;
using SkiaSharp;
using static ScreenDriver.Themes.ThemeHelpers;

namespace ScreenDriver.Themes;

public record EvaTheme() : Theme(ScreenLayoutMode.Portrait, BuildWidgets())
{
    private const string Name = "eva";

    // Font sizes
    private const float UsageFontSize = 20f;
    private const float TempFontSize = 14f;
    private const float NetworkFontSize = 13f;

    // Colors
    private static readonly SKColor PrimaryColor = new(57, 19, 59);

    // Sizing reference texts
    private const string UsageSizeText = "100%";
    private const string TempSizeText = "999\u00b0C";
    private const string NetworkSizeText = "999.99";

    private static Widget[] BuildWidgets()
    {
        var (width, height) = ScreenDevice.DimensionsFor(ScreenLayoutMode.Portrait);
        var dir = Path.Combine(ThemesRoot, Name);
        var imagePath = DiscoverSingle(dir, ImageExtensions);
        var fontPaths = DiscoverAll(dir, FontExtensions);
        var typeface = fontPaths.Length == 1 ? SKTypeface.FromFile(fontPaths[0]) : SKTypeface.Default;
        var background = ScreenBackground.FromImage(imagePath, width, height);
        return
        [
            new BackgroundWidget(background),

            // Clock
            new ClockWidget(ComputeZone(120, 20, "00:00", 32f, typeface),
                TimeSpan.FromSeconds(3),
                background, typeface, 32f, PrimaryColor),


            // CPU panel 
            new CpuUsageWidget(
                ComputeZone(45, 160, UsageSizeText, UsageFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, UsageFontSize, PrimaryColor),

            new CpuTempWidget(
                ComputeZone(45, 185, TempSizeText, TempFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, TempFontSize, PrimaryColor),

            // GPU panel (top-right)
            new GpuUsageWidget(
                ComputeZone(130, 260, UsageSizeText, UsageFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, UsageFontSize, PrimaryColor),

            new GpuTempWidget(
                ComputeZone(130, 285, TempSizeText, TempFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, TempFontSize, PrimaryColor),


            // Network (bottom strip)
            new NetworkWidget(
                ComputeZone(285, 330, NetworkSizeText, NetworkFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, NetworkFontSize, PrimaryColor,
                NetworkDirection.Down, true),

            new NetworkWidget(
                ComputeZone(285, 410, NetworkSizeText, NetworkFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, NetworkFontSize, PrimaryColor,
                NetworkDirection.Up, true),
            
            // memory
            new MemoryGaugeWidget(
                new WidgetZone(80, 370, 100, 100) ,
                TimeSpan.FromSeconds(5),
                background,
                PrimaryColor,
                30f,
                10f,
                new SKColor(180, 57, 69),
                new GaugeLabel(typeface, 20f, PrimaryColor)
            ),
        
        ];
    }
}