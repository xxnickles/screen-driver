using ScreenDriver.Controller;
using ScreenDriver.Controller.Events;
using ScreenDriver.Themes;
using ScreenDriver.Tui;

var port = args.Length > 0 ? args[0] : null;

var registry = new ThemeRegistry("default", new Dictionary<string, Func<Theme>>
{
    ["default"] = () => new DefaultTheme(),
    ["static"] = () => new StaticTheme(),
});

var bus = new EventBus();

await using var controller = new ScreenController(registry, bus, port);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await controller.StartAsync(cts.Token);

await ScreenDriverApp.Run(controller, registry, bus, cts.Token);

await controller.Stop();
