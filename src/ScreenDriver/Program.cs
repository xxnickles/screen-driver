using ScreenDriver;
using ScreenDriver.Controller;
using ScreenDriver.Controller.Events;
using ScreenDriver.Themes;

var port = args.Length > 0 ? args[0] : null;
var theme = new DefaultTheme();
// var theme = new StaticTheme();
var bus = new EventBus();

await using var controller = new ScreenController(theme, bus, port);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

_ = Task.Run(async () =>
{
    await foreach (var e in bus.ReadAllAsync(cts.Token))
    {
        var prefix = e switch
        {
            Error   => "[ERR]",
            Warning => "[WRN]",
            _       => "[INF]",
        };
        Console.WriteLine($"{prefix} [{e.Source}] {e.Message}");
    }
}, cts.Token);

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
await controller.Stop();
Console.WriteLine("Done.");
