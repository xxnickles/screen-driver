using ScreenDriver;
using ScreenDriver.Meters;
using ScreenDriver.Stats;
using ScreenDriver.Themes;
using ScreenDriver.Widgets;
using SkiaSharp;

// Take a baseline CPU reading so the first real render has a delta
CpuStats.GetUsagePercent();

var port = args.Length > 0 ? args[0] : null;
var themesRoot = Path.Combine(AppContext.BaseDirectory, "templates");
var theme = Theme.Load(themesRoot, "default", 480, 320);

// Meters
var cpuUsage = new CpuUsageMeter();
var cpuTemp = new CpuTempMeter();
var memory = new MemoryMeter();
var gpuUsage = new GpuUsageMeter();
var gpuTemp = new GpuTempMeter();
var disk = new DiskMeter();

// Widgets — coordinates are center-anchor (X, Y) for text, top-left for bars
// Screen is 480×320 in landscape
Widget[] widgets =
[
    new BackgroundWidget(theme.Background),

    new TextWidget(120, 35, cpuUsage, theme.Background,
        SKColors.White, 18f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    new TextWidget(360, 35, cpuTemp, theme.Background,
        SKColors.White, 18f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    new BarWidget(0, 70, 200, 40, memory, theme.Background,
        SKColors.DodgerBlue,
        TimeSpan.FromSeconds(5)),

    new TextWidget(100, 135, memory, theme.Background,
        SKColors.White, 15f,
        TimeSpan.FromSeconds(5),
        typeface: theme.Typeface),

    new TextWidget(240, 185, disk, theme.Background,
        SKColors.White, 15f,
        TimeSpan.FromMinutes(1),
        typeface: theme.Typeface),

    new TextWidget(40, 235, gpuUsage, theme.Background,
        SKColors.White, 20f,
        TimeSpan.FromSeconds(2),
        typeface: theme.Typeface),

    new TextWidget(120, 235, gpuTemp, theme.Background,
        SKColors.White, 20f,
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
catch (OperationCanceledException) { }

Console.WriteLine("Shutting down...");
await controller.StopAsync();
Console.WriteLine("Done.");
