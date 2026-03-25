using ScreenDriver;
using ScreenDriver.Meters;
using ScreenDriver.Stats;
using ScreenDriver.Themes;
using ScreenDriver.Widgets;
using SkiaSharp;

// Take baseline readings so the first real render has a delta
CpuStats.GetUsagePercent();
NetworkStats.GetSpeed();

var port = args.Length > 0 ? args[0] : null;
var themesRoot = Path.Combine(AppContext.BaseDirectory, "templates");
var theme = Theme.Load(themesRoot, "default", 480, 320);

// Meters
var cpuUsage = new CpuUsageMeter();
var cpuTemp = new CpuTempMeter();
var gpuUsage = new GpuUsageMeter();
var gpuTemp = new GpuTempMeter();
var memory = new MemoryMeter();
var disk = new DiskMeter();
var netUp = new NetworkUpMeter();
var netDown = new NetworkDownMeter();

Widget[] widgets =
[
    new BackgroundWidget(theme.Background),

    // CPU panel (top-left)
    new TextWidget(75, 80, cpuUsage, theme.Background,
        SKColors.White, 34f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    new TextWidget(75, 130, cpuTemp, theme.Background,
        SKColors.White, 18f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    // GPU panel (top-right)
    new TextWidget(240, 80, gpuUsage, theme.Background,
        SKColors.White, 34f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    new TextWidget(240, 130, gpuTemp, theme.Background,
        SKColors.White, 18f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    // Drives panel (left-middle, stacked)
    new TextWidget(400, 20, disk, theme.Background,
        SKColors.DodgerBlue, 13f,
        TimeSpan.FromMinutes(1),
        typeface: theme.Typeface),

    // Memory panel (middle, wide)
    new BarWidget(115, 195, 140, 15, memory, theme.Background,
        SKColors.DodgerBlue,
        TimeSpan.FromSeconds(5),
        SKColors.Azure),

    new TextWidget(190, 220, memory, theme.Background,
        SKColors.White, 17f,
        TimeSpan.FromSeconds(5),
        typeface: theme.Typeface),

    // Network (bottom strip)
    new TextWidget(110, 280, netDown, theme.Background,
        SKColors.White, 13f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    new TextWidget(240, 280, netUp, theme.Background,
        SKColors.White, 13f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),
];

await using var controller = new ScreenController(widgets, port);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await controller.StartAsync(cts.Token);
Console.WriteLine("Press Ctrl+C to exit.");

try
{
    await Task.Delay(Timeout.Infinite, cts.Token);
}
catch (OperationCanceledException)
{
}

Console.WriteLine("Shutting down...");
await controller.StopAsync();
Console.WriteLine("Done.");