using ScreenDriver.Widgets;

namespace ScreenDriver.Scheduler;

/// <summary>
/// Runs a PeriodicTimer per widget, rendering frames at each widget's interval.
/// Fires FrameRendered when a widget produces a new frame.
/// Supports pause/resume for disconnect handling — timers keep running but renders are skipped.
/// </summary>
public sealed class WidgetScheduler
{
    private readonly IEnumerable<Widget> _widgets;
    private readonly List<Task> _tasks = [];
    private CancellationTokenSource? _cts;
    private volatile bool _paused;

    /// <summary>
    /// Raised when a widget produces a new frame.
    /// </summary>
    public event Action<WidgetZone, Rgb565Frame>? FrameRendered;

    public WidgetScheduler(IEnumerable<Widget> widgets)
    {
        _widgets = widgets;
    }

    public void Pause() => _paused = true;
    public void Resume() => _paused = false;

    public void StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        foreach (var widget in _widgets)
            _tasks.Add(Task.Run(() => RunWidgetAsync(widget, _cts.Token)));
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            await Task.WhenAll(_tasks);
            _cts.Dispose();
        }
    }

    private async Task RunWidgetAsync(Widget widget, CancellationToken ct)
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
        if (_paused) return;

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
