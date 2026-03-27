using ScreenDriver;
using ScreenDriver.Themes;

var port = args.Length > 0 ? args[0] : null;
 var theme = new DefaultTheme();
// var theme = new StaticTheme();

await using var controller = new ScreenController(theme, port);

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
