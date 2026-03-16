using ScreenDriver.Queue;
using ScreenDriver.Widgets;

namespace ScreenDriver.Scheduler;

/// <summary>
/// Runs a PeriodicTimer per widget, rendering and enqueuing frames at each widget's interval.
/// </summary>
public sealed class WidgetScheduler
{
    private readonly ScreenWriteQueue _queue;
    private readonly IEnumerable<Widget> _widgets;
    private readonly List<Task> _tasks = [];
    private CancellationTokenSource? _cts;

    public WidgetScheduler(ScreenWriteQueue queue, IEnumerable<Widget> widgets)
    {
        _queue = queue;
        _widgets = widgets;
    }

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
        EnqueueRender(widget);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                EnqueueRender(widget);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void EnqueueRender(Widget widget)
    {
        try
        {
            var frame = widget.Render();
            _queue.Enqueue(new WriteRequest(widget.Zone, frame));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Render failed for widget at ({widget.Zone.X},{widget.Zone.Y}): {ex.Message}");
        }
    }
}
