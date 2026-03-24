using ScreenDriver.Widgets;

namespace ScreenDriver.Scheduler;

/// <summary>
/// Runs a PeriodicTimer per widget, rendering frames at each widget's interval.
/// Fires FrameRendered when a widget produces a new frame.
/// StopAsync/Start lifecycle: stopping cancels all timers, starting recreates them.
/// All widgets re-render immediately on start, ensuring backgrounds and state are fresh.
/// </summary>
public sealed class WidgetScheduler
{
    private readonly IEnumerable<Widget> _widgets;
    private List<Task> _tasks = [];
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Raised when a widget produces a new frame.
    /// </summary>
    public event Action<WidgetZone, Rgb565Frame>? FrameRendered;

    public WidgetScheduler(IEnumerable<Widget> widgets)
    {
        _widgets = widgets;
    }

    public void Start(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _tasks = [];
        foreach (var widget in _widgets)
            _tasks.Add(Task.Run(() => RunWidget(widget, _cts.Token), ct));
    }

    public async Task Stop()
    {
        if (_cts is null) return;

        await _cts.CancelAsync();
        await Task.WhenAll(_tasks);
        _cts.Dispose();
        _cts = null;
        _tasks = [];
    }

    private async Task RunWidget(Widget widget, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(widget.Interval);

        // Render immediately on first tick
        EmitRender(widget);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                EmitRender(widget);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void EmitRender(Widget widget)
    {
        try
        {
            var frame = widget.Render();
            FrameRendered?.Invoke(widget.Zone, frame);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Render failed for widget at ({widget.Zone.X},{widget.Zone.Y}): {ex.Message}");
        }
    }
}
