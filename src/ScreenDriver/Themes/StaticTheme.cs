using ScreenDriver.Device;
using ScreenDriver.Rendering;
using ScreenDriver.Widgets;
using SkiaSharp;
using static ScreenDriver.Themes.ThemeHelpers;

namespace ScreenDriver.Themes;

public record StaticTheme() : Theme(ScreenLayoutMode.Landscape, BuildWidgets())
{
    private const string Name = "static";

    // Font sizes
    private const float LabelFontSize = 37f;
    private const float UsageFontSize = 34f;
    private const float UsageZoneFontSize = 38f;
    private const float TempFontSize = 26f;
    private const float TempZoneFontSize = 24f;
    private const float DiskLabelFontSize = 18f;
    private const float DiskValueFontSize = 16f;
    private const float MemoryFontSize = 21f;
    private const float NetworkFontSize = 21f;
    private const float TimesFontSize = 15f;

    // Colors
    private static readonly SKColor PrimaryColor = SKColors.Black;
    private static readonly SKColor DiskLabelColor = SKColors.WhiteSmoke;
    private static readonly SKColor MemoryBarFillColor = SKColors.DodgerBlue;
    private static readonly SKColor MemoryBarTrackColor = SKColors.Azure;

    // Sizing reference texts
    private const string UsageSizeText = "100%";
    private const string TempSizeText = "999\u00b0C";
    private const string DiskSizeText = "99999 / 99999 GB";
    private const string MemorySizeText = "99999 / 99999 MB";
    private const string NetworkSizeText = "999.9 MB/s";

    // Disk Y
    private const int DiskY = 30;

    private static Widget[] BuildWidgets()
    {
        var (width, height) = ScreenDevice.DimensionsFor(ScreenLayoutMode.Landscape);
        var dir = Path.Combine(ThemesRoot, Name);
        var fontPaths = DiscoverAll(dir, FontExtensions);
        var typeface = fontPaths.Length == 1 ? SKTypeface.FromFile(fontPaths[0]) : SKTypeface.Default;
        var background =
            ScreenBackground.SolidColor(SKColors.Crimson, width, height);

        return
        [
            new BackgroundWidget(background),

            new DateWidget(ComputeZone(70, 15, "12/31/9999", TimesFontSize, typeface), TimeSpan.FromHours(1),
                background, typeface, TimesFontSize, PrimaryColor),
            
            new ClockWidget(ComputeZone(435, 15, "23:59", TimesFontSize, typeface), TimeSpan.FromSeconds(10),
                background, typeface, TimesFontSize, PrimaryColor),

            // CPU panel (top-left)
            new TextWidget(
                ComputeZone(75, 30, "CPU", LabelFontSize, typeface),
                background, typeface, LabelFontSize, PrimaryColor, "CPU"),
            new CpuUsageWidget(
                ComputeZone(75, 70, UsageSizeText, UsageZoneFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, UsageFontSize, PrimaryColor),

            new CpuTempWidget(
                ComputeZone(75, 120, TempSizeText, TempZoneFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, TempFontSize, PrimaryColor),

            // GPU panel (top-right)
            new TextWidget(
                ComputeZone(240, 30, "GPU", LabelFontSize, typeface),
                background, typeface, LabelFontSize, PrimaryColor, "GPU"),

            new GpuUsageWidget(
                ComputeZone(240, 70, UsageSizeText, UsageZoneFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, UsageFontSize, PrimaryColor),

            new GpuTempWidget(
                ComputeZone(240, 120, TempSizeText, TempFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, TempZoneFontSize, PrimaryColor),

            // Drives panel (right side, one widget per drive)
            new TextWidget(
                ComputeZone(380, DiskY, "Disks", LabelFontSize, typeface),
                background, typeface, LabelFontSize, PrimaryColor, "disks"),
            new DiskWidget(
                DiskWidget.ComputeZone(380, DiskY + 60, typeface, DiskLabelFontSize, DiskValueFontSize, DiskSizeText),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: DiskLabelFontSize, valueFontSize: DiskValueFontSize,
                labelColor: DiskLabelColor, valueColor: PrimaryColor,
                driveIndex: 0),
            new DiskWidget(
                DiskWidget.ComputeZone(380, DiskY + 105, typeface, DiskLabelFontSize, DiskValueFontSize, DiskSizeText),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: DiskLabelFontSize, valueFontSize: DiskValueFontSize,
                labelColor: DiskLabelColor, valueColor: PrimaryColor,
                driveIndex: 1),
            new DiskWidget(
                DiskWidget.ComputeZone(380, DiskY + 150, typeface, DiskLabelFontSize, DiskValueFontSize, DiskSizeText),
                TimeSpan.FromMinutes(1),
                background, typeface,
                labelFontSize: DiskLabelFontSize, valueFontSize: DiskValueFontSize,
                labelColor: DiskLabelColor, valueColor: PrimaryColor,
                driveIndex: 2),

            // Memory panel (middle, wide)
            new TextWidget(
                ComputeZone(150, 160, "RAM", LabelFontSize, typeface),
                background, typeface, LabelFontSize, PrimaryColor, "RAM"),

            new MemoryBarWidget(
                new WidgetZone(30, 215, 240, 15),
                TimeSpan.FromSeconds(5),
                background,
                MemoryBarFillColor, MemoryBarTrackColor),

            new MemoryTextWidget(
                ComputeZone(150, 230, MemorySizeText, MemoryFontSize, typeface),
                TimeSpan.FromSeconds(5),
                background, typeface, MemoryFontSize, PrimaryColor),

            // Network (bottom strip)

            new TextWidget(
                ComputeZone(50, 265, "DN", LabelFontSize, typeface),
                background, typeface, LabelFontSize, PrimaryColor, "DN"),
            new NetworkWidget(
                ComputeZone(155, 275, NetworkSizeText, NetworkFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, NetworkFontSize, PrimaryColor,
                NetworkDirection.Down),
            new TextWidget(
                ComputeZone(290, 265, "UP", LabelFontSize, typeface),
                background, typeface, LabelFontSize, PrimaryColor, "UP"),
            new NetworkWidget(
                ComputeZone(400, 275, NetworkSizeText, NetworkFontSize, typeface),
                TimeSpan.FromSeconds(2),
                background, typeface, NetworkFontSize, PrimaryColor,
                NetworkDirection.Up),
        ];
    }
}