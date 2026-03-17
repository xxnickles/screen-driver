using ScreenDriver;
using ScreenDriver.Queue;
using ScreenDriver.Scheduler;
using ScreenDriver.Stats;
using ScreenDriver.Widgets;
using SkiaSharp;

using var screen = args.Length > 0
    ? new ScreenDevice(args[0])
    : ScreenDevice.AutoDetect();

Console.WriteLine("Opening connection to screen...");
var sizeId = screen.Initialize();
Console.WriteLine($"Screen responded with size ID: 0x{sizeId:X2} ({sizeId})");

screen.SetBrightness(0);
screen.SetOrientation(ScreenOrientation.Landscape);

Console.WriteLine("Filling screen with black...");
screen.FillScreen(0, 0, 0);

// Take a baseline CPU reading so the first real render has a delta
CpuStats.GetUsagePercent();

var queue = new ScreenWriteQueue(screen);
using var cts = new CancellationTokenSource();
queue.StartAsync(cts.Token);

var w = screen.Width;

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

var scheduler = new WidgetScheduler(queue, widgets);
scheduler.StartAsync(cts.Token);

Console.WriteLine("Widgets running. Press Ctrl+C to exit.");

var tcs = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    tcs.TrySetResult();
};
await tcs.Task;

Console.WriteLine("Shutting down...");
await scheduler.StopAsync();
await queue.StopAsync();
screen.ScreenOff();
Console.WriteLine("Done.");
