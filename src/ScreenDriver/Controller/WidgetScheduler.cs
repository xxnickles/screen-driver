using ScreenDriver.Controller.Events;
using ScreenDriver.Rendering;
using ScreenDriver.Widgets;

namespace ScreenDriver.Controller;

/// <summary>
/// Runs a render loop per widget, ticking aligned to each widget's interval boundary
/// (cron-like: a 1-minute widget updates on the minute, not at an arbitrary phase).
/// The delay to the next boundary is recomputed every tick, so it self-corrects drift.
/// Fires FrameRendered when a widget produces a new frame.
/// StopAsync/Start lifecycle: stopping cancels all loops, starting recreates them.
/// All widgets re-render immediately on start, ensuring backgrounds and state are fresh.
/// </summary>
public sealed class WidgetScheduler
{
    private readonly IEnumerable<Widget> _widgets;
    private readonly EventBus _bus;
    private List<Task> _tasks = [];
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Raised when a widget produces a new frame.
    /// </summary>
    public event Action<WidgetZone, Rgb565Frame>? FrameRendered;

    public WidgetScheduler(IEnumerable<Widget> widgets, EventBus bus)
    {
        _widgets = widgets;
        _bus = bus;
    }

    public void Start(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _tasks = [];
        foreach (var widget in _widgets)
        {
            var name = widget.GetType().Name;
            widget.EventRaised = message => _bus.Publish(new Info(name, message));
            _tasks.Add(Task.Run(() => RunWidget(widget, _cts.Token), ct));
        }
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
        var name = widget.GetType().Name;
        _bus.Publish(new Info(name, "started"));

        // Render immediately on start
        EmitRender(widget);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(DelayToNextBoundary(widget.Interval), ct);
                EmitRender(widget);
            }
        }
        catch (OperationCanceledException)
        {
            _bus.Publish(new Info(name, "stopped"));
        }
    }

    /// <summary>
    /// Time from now until the next interval boundary, measured against the start of the day
    /// (e.g. a 1-minute interval lands on each :00 second). Recomputed each tick to absorb
    /// render and scheduling jitter so ticks stay aligned over long runs.
    /// </summary>
    private static TimeSpan DelayToNextBoundary(TimeSpan interval)
    {
        long step = interval.Ticks;
        long sinceMidnight = DateTime.Now.TimeOfDay.Ticks;
        return TimeSpan.FromTicks(step - (sinceMidnight % step));
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
            _bus.Publish(new Error(widget.GetType().Name, ex));
        }
    }
}
