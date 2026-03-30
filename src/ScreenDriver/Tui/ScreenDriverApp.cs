using ScreenDriver.Controller;
using ScreenDriver.Controller.Events;
using ScreenDriver.Themes;
using Spectre.Console;

namespace ScreenDriver.Tui;

public static class ScreenDriverApp
{
    public static async Task Run(ScreenController controller, ThemeRegistry registry, EventBus bus, CancellationToken ct)
    {
        var logPanel = new LogPanel();
        var commandPanel = new CommandPanel(controller, registry);

        logPanel.StartConsuming(bus, ct);

        while (!ct.IsCancellationRequested)
        {
            // Live mode: show log, listen for Esc
            var escapeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Background task to watch for Esc key
            _ = Task.Run(() =>
            {
                while (!escapeCts.Token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            logPanel.StopLive();
                            break;
                        }
                    }
                    Thread.Sleep(50);
                }
            }, escapeCts.Token);

            logPanel.RunLive(ct);

            if (ct.IsCancellationRequested)
                break;

            // Menu mode
            AnsiConsole.Clear();
            await commandPanel.ShowMenu();
            AnsiConsole.Clear();

            await escapeCts.CancelAsync();
        }
    }
}
