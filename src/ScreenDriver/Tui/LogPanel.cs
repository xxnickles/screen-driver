using ScreenDriver.Controller.Events;
using Spectre.Console;

namespace ScreenDriver.Tui;

public class LogPanel
{
    private readonly List<string> _lines = [];
    private readonly Lock _lock = new();
    private CancellationTokenSource? _liveCts;

    public void StartConsuming(EventBus bus, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            await foreach (var e in bus.ReadAllAsync(ct))
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var (prefix, color) = e switch
                {
                    Error => ("[ERR]", "red"),
                    Warning => ("[WRN]", "yellow"),
                    _ => ("[INF]", "grey"),
                };
                var source = Markup.Escape(e.Source);
                var message = Markup.Escape(e.Message);
                var line = $"[white]{timestamp}[/] [{color}]{Markup.Escape(prefix)}[/] [{color}][[{source}]][/] {message}";

                lock (_lock)
                {
                    _lines.Add(line);
                    // Keep a reasonable buffer
                    if (_lines.Count > 500)
                        _lines.RemoveAt(0);
                }
            }
        }, ct);
    }

    /// <summary>
    /// Runs the live display until the provided token is cancelled (e.g., Esc pressed).
    /// </summary>
    public void RunLive(CancellationToken ct)
    {
        _liveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        AnsiConsole.Live(new Panel("Starting...") { Header = new PanelHeader("Log") })
            .AutoClear(false)
            .Start(ctx =>
            {
                while (!_liveCts.Token.IsCancellationRequested)
                {
                    string[] snapshot;
                    lock (_lock)
                    {
                        // Show last N lines that fit
                        var height = Math.Max(Console.WindowHeight - 6, 5);
                        var skip = Math.Max(0, _lines.Count - height);
                        snapshot = _lines.Skip(skip).ToArray();
                    }

                    var content = snapshot.Length > 0
                        ? string.Join("\n", snapshot)
                        : "[grey]Waiting for events...[/]";

                    var panel = new Panel(new Markup(content))
                    {
                        Header = new PanelHeader("[blue]Log[/] [grey](Esc: menu | Ctrl+C: quit)[/]"),
                        Expand = true,
                    };

                    ctx.UpdateTarget(panel);
                    Thread.Sleep(250); // refresh rate
                }
            });
    }

    public void StopLive() => _liveCts?.Cancel();
}
