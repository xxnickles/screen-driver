using ScreenDriver.Widgets;
using SkiaSharp;
using static ScreenDriver.Themes.ThemeHelpers;

namespace ScreenDriver.Themes;

public record DefaultTheme : Theme
{
    private const string Name = "default";

    // Font sizes
    private const float UsageFontSize = 34f;
    private const float TempFontSize = 18f;
    private const float DiskLabelFontSize = 16f;
    private const float DiskValueFontSize = 13f;
    private const float MemoryFontSize = 17f;
    private const float NetworkFontSize = 13f;

    // Colors
    private static readonly SKColor PrimaryColor = SKColors.White;
    private static readonly SKColor DiskLabelColor = new (253,89,88);
    private static readonly SKColor DiskValueColor = SKColors.White;
    private static readonly SKColor MemoryBarFillColor =  new (7,108,224);
    private static readonly SKColor MemoryBarTrackColor = SKColors.White;

    // Sizing reference texts
    private const string UsageSizeText = "100%";
    private const string TempSizeText = "999\u00b0C";
    private const string DiskSizeText = "9999 / 9999 GB";
    private const string MemorySizeText = "99999 / 99999 MB";
    private const string NetworkSizeText = "999.9 MB/s";

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
                ComputeZone(75, 80, UsageSizeText, UsageFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, UsageFontSize, PrimaryColor),

            new CpuTempWidget(
                ComputeZone(75, 130, TempSizeText, TempFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, TempFontSize, PrimaryColor),

            // GPU panel (top-right)
            new GpuUsageWidget(
                ComputeZone(240, 80, UsageSizeText, UsageFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, UsageFontSize, PrimaryColor),

            new GpuTempWidget(
                ComputeZone(240, 130, TempSizeText, TempFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, TempFontSize, PrimaryColor),

            // Drives panel (right side, one widget per drive)
            new DiskWidget(
                DiskWidget.ComputeZone(400, 20, typeface, DiskLabelFontSize, DiskValueFontSize, DiskSizeText),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: DiskLabelFontSize, valueFontSize: DiskValueFontSize,
                labelColor: DiskLabelColor, valueColor: DiskValueColor,
                driveIndex: 0),
            new DiskWidget(
                DiskWidget.ComputeZone(400, 65, typeface, DiskLabelFontSize, DiskValueFontSize, DiskSizeText),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: DiskLabelFontSize, valueFontSize: DiskValueFontSize,
                labelColor: DiskLabelColor, valueColor: DiskValueColor,
                driveIndex: 1),
            new DiskWidget(
                DiskWidget.ComputeZone(400, 110, typeface, DiskLabelFontSize, DiskValueFontSize, DiskSizeText),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: DiskLabelFontSize, valueFontSize: DiskValueFontSize,
                labelColor: DiskLabelColor, valueColor: DiskValueColor,
                driveIndex: 2),

            // Memory panel (middle, wide)
            new MemoryBarWidget(
                new WidgetZone(115, 195, 140, 15),
                TimeSpan.FromSeconds(5),
                background,
                MemoryBarFillColor, MemoryBarTrackColor),

            new MemoryTextWidget(
                ComputeZone(190, 220, MemorySizeText, MemoryFontSize, typeface),
                TimeSpan.FromSeconds(5),
                background, typeface, MemoryFontSize, PrimaryColor),

            // Network (bottom strip)
            new NetworkWidget(
                ComputeZone(110, 280, NetworkSizeText, NetworkFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, NetworkFontSize, PrimaryColor,
                NetworkDirection.Down),

            new NetworkWidget(
                ComputeZone(240, 280, NetworkSizeText, NetworkFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, NetworkFontSize, PrimaryColor,
                NetworkDirection.Up),
            
            // Clock
            new ClockWidget(ComputeZone(400, 180, "23:59", TempFontSize, typeface), 
                TimeSpan.FromSeconds(5),
                background, typeface, TempFontSize, PrimaryColor)
        ];
    }
}
