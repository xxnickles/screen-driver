using ScreenDriver.Commands;
using ScreenDriver.Scheduler;
using ScreenDriver.Widgets;

namespace ScreenDriver;

/// <summary>
/// Top-level coordinator that owns device lifecycle, command queue, and widget scheduling.
/// Handles disconnect detection (via queue callback) and auto-reconnect via DeviceScanner.
/// External code can submit commands via EnqueueCommand.
/// </summary>
public sealed class ScreenController : IAsyncDisposable
{
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(5);

    private readonly string? _fixedPort;
    private readonly ScreenCommandQueue _queue;
    private readonly WidgetScheduler _scheduler;
    private CancellationTokenSource? _cts;
    private ScreenDevice? _device;
    private volatile bool _reconnecting;

    public ScreenController(IEnumerable<Widget> widgets, string? port = null)
    {
        _fixedPort = port;
        _queue = new ScreenCommandQueue(() => _device, OnDisconnect);
        _scheduler = new WidgetScheduler(widgets);
        _scheduler.FrameRendered += (zone, frame) =>
            EnqueueCommand(new DisplayBitmapCommand(zone, frame));
    }

    /// <summary>
    /// Submits a command to the processing queue.
    /// Commands are dropped if the screen is disconnected.
    /// </summary>
    public void EnqueueCommand(ScreenCommand command) => _queue.Enqueue(command);

    /// <summary>
    /// Connects to the screen (polling if not found), initializes, and starts
    /// the command queue and widget scheduler.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await ConnectAsync(_cts.Token);

        _queue.Start(_cts.Token);
        _scheduler.StartAsync(_cts.Token);

        Console.WriteLine("Widgets running.");
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;

        await _cts.CancelAsync();
        await _scheduler.StopAsync();
        await _queue.StopAsync();

        if (_device is not null)
        {
            try { _device.ScreenOff(); } catch { /* device may already be gone */ }
            _device.Dispose();
            _device = null;
        }

        _cts.Dispose();
        _cts = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    /// <summary>
    /// Polls for the screen until found, then opens and initializes it.
    /// </summary>
    private async Task ConnectAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var port = _fixedPort ?? DeviceScanner.FindScreen();

            if (port is not null)
            {
                try
                {
                    var device = new ScreenDevice(port);
                    var sizeId = device.Initialize();
                    Console.WriteLine($"Screen connected on {port} (size ID: 0x{sizeId:X2})");

                    device.SetBrightness(0);
                    device.SetOrientation(ScreenOrientation.Landscape);
                    device.FillScreen(0, 0, 0);

                    _device = device;
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed to initialize screen on {port}: {ex.Message}");
                }
            }

            Console.WriteLine("Screen not found, retrying in 5 seconds...");
            await Task.Delay(ReconnectInterval, ct);
        }
    }

    private void OnDisconnect()
    {
        if (_reconnecting) return;
        _reconnecting = true;

        Console.Error.WriteLine("Screen disconnected. Pausing widgets and scanning for reconnect...");
        _scheduler.Pause();

        // Dispose old device
        try { _device?.Dispose(); } catch { /* already gone */ }
        _device = null;

        // Fire-and-forget reconnect loop
        _ = ReconnectLoopAsync(_cts?.Token ?? CancellationToken.None);
    }

    private async Task ReconnectLoopAsync(CancellationToken ct)
    {
        try
        {
            await ConnectAsync(ct);

            Console.WriteLine("Screen reconnected. Resuming widgets.");
            _scheduler.Resume();
        }
        catch (OperationCanceledException)
        {
            // Shutting down — don't resume
        }
        finally
        {
            _reconnecting = false;
        }
    }
}
