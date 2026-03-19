using System.Text;
using ScreenDriver;
using ScreenDriver.Stats;
using ScreenDriver.Widgets;
using SkiaSharp;

// Take a baseline CPU reading so the first real render has a delta
CpuStats.GetUsagePercent();

var port = args.Length > 0 ? args[0] : null;

// Widget width adapts to orientation (480 for landscape, 320 for portrait)
// set inside ScreenController after orientation is applied.
var w = ScreenDevice.NativeHeight; // landscape width
var sb = new StringBuilder();
Widget[] widgets =
[
    new TextWidget(
        new WidgetZone(0, 0, w / 2, 70),
        TimeSpan.FromSeconds(2),
        () => $"CPU: {CpuStats.GetUsagePercent():F0}%",
        SKColors.Black, SKColors.White, 18f),
    
    new TextWidget(
        new WidgetZone(w / 2, 0, w / 2, 70),
        TimeSpan.FromSeconds(2),
        () => $"Temp: {CpuStats.GetTemperatureCelsius()}",
        SKColors.Black, SKColors.White, 18f),

    new BarWidget(
        new WidgetZone(0, 70, w, 40),
        TimeSpan.FromSeconds(5),
        () => MemoryStats.GetUsagePercent().UsedPercent,
        SKColors.DarkSlateGray, SKColors.DodgerBlue, SKColors.White, 15f),

    new TextWidget(
        new WidgetZone(0, 110, w, 50),
        TimeSpan.FromSeconds(5),
        () =>
        {
            var (_, totalMb, usedMb) = MemoryStats.GetUsagePercent();
            return $"Mem: {usedMb}MB / {totalMb}MB";
        },
        SKColors.Black, SKColors.White, 15f),
    
    new TextWidget(
        new WidgetZone(0, 160, w, 50), TimeSpan.FromMinutes(1),
        () =>
        {
            sb.Clear();
            var diskStats = DiskStats.GetUsage();
            foreach (var diskInfo in diskStats)
            {
                var usedGb = diskInfo.UsedBytes / (1024.0 * 1024 * 1024);
                var totalGb = diskInfo.TotalBytes / (1024.0 * 1024 * 1024);
                sb.AppendLine($"{diskInfo.Label}: {usedGb:F0}GB / {totalGb:F0}GB");
            }

            return sb.ToString();
        }, 
        SKColors.Black, SKColors.White, 15f),
    new TextWidget(
        new WidgetZone(0, 210, w / 2, 50),
        TimeSpan.FromSeconds(2),
        () => $"GPU: {GpuStats.GetUsagePercent():F0}%",
        SKColors.Black, SKColors.White, 15f),
    
    new TextWidget(
        new WidgetZone(w / 2, 210, w / 2, 50),
        TimeSpan.FromSeconds(2),
        () => $"Temp: {GpuStats.GetTemperatureCelsius()}",
        SKColors.Black, SKColors.White, 15f),
    
    
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
