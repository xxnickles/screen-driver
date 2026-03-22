using ScreenDriver;
using ScreenDriver.Meters;
using ScreenDriver.Stats;
using ScreenDriver.Widgets;
using SkiaSharp;

// Take a baseline CPU reading so the first real render has a delta
CpuStats.GetUsagePercent();

var port = args.Length > 0 ? args[0] : null;
var backgroundPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "templates", "default", "band-maid-background.png"));
var background = new BackgroundWidget(backgroundPath, 480, 320);

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
    new TextWidget(120, 35, cpuUsage,
        SKColors.Black, SKColors.White, 18f,
        TimeSpan.FromSeconds(2)),

    new TextWidget(360, 35, cpuTemp,
        SKColors.Black, SKColors.White, 18f,
        TimeSpan.FromSeconds(2)),

    new BarWidget(0, 70, 200, 40, memory,
        SKColors.DarkSlateGray, SKColors.DodgerBlue,
        TimeSpan.FromSeconds(5)),

    new TextWidget(100, 135, memory,
        SKColors.Black, SKColors.White, 15f,
        TimeSpan.FromSeconds(5)),

    new TextWidget(240, 185, disk,
        SKColors.Black, SKColors.White, 15f,
        TimeSpan.FromMinutes(1)),

    new TextWidget(40, 235, gpuUsage,
        SKColors.Transparent, SKColors.White, 20f,
        TimeSpan.FromSeconds(2)),

    new TextWidget(120, 235, gpuTemp,
        SKColors.Transparent, SKColors.White, 20f,
        TimeSpan.FromSeconds(2)),
];

await using var controller = new ScreenController(widgets, port, background);

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
