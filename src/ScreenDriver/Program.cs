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

Widget[] widgets =
[
    new TextWidget(
        new WidgetZone(0, 0, w, 70),
        TimeSpan.FromSeconds(2),
        () => $"CPU: {CpuStats.GetUsagePercent():F0}%",
        SKColors.Black, SKColors.White, 24f),

    new BarWidget(
        new WidgetZone(0, 80, w, 70),
        TimeSpan.FromSeconds(5),
        () => MemoryStats.GetUsagePercent().UsedPercent,
        SKColors.DarkSlateGray, SKColors.DodgerBlue, SKColors.White, 20f),

    new TextWidget(
        new WidgetZone(0, 150, w, 60),
        TimeSpan.FromSeconds(5),
        () =>
        {
            var (_, totalMb, usedMb) = MemoryStats.GetUsagePercent();
            return $"Mem: {usedMb}MB / {totalMb}MB";
        },
        SKColors.Black, SKColors.White, 18f),
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
