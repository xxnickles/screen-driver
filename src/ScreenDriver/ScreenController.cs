using ScreenDriver.Commands;
using ScreenDriver.Scheduler;
using ScreenDriver.Themes;
using ScreenDriver.Widgets;

namespace ScreenDriver;

/// <summary>
/// Top-level coordinator that owns device lifecycle, command queue, and widget scheduling.
/// Handles disconnect detection (via queue event) and auto-reconnect via DeviceScanner.
/// External code can submit commands via EnqueueCommand.
/// </summary>
public sealed class ScreenController : IAsyncDisposable
{
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(5);

    private readonly string? _fixedPort;
    private readonly ScreenCommandQueue _commandQueue;
    private readonly WidgetScheduler _scheduler;
    private CancellationTokenSource? _cts;
    private ScreenDevice? _device;
    private volatile bool _reconnecting;

    public ScreenController(Theme theme, string? port = null)
    {
        _fixedPort = port;
        _commandQueue = new ScreenCommandQueue(() => _device);
        _commandQueue.Disconnected += OnDisconnect;
        _scheduler = new WidgetScheduler(theme.Widgets);
        // Enqueue frame render events to the command queue to be presented in the screen
        _scheduler.FrameRendered += (zone, frame) =>
            EnqueueCommand(new DisplayBitmapCommand(zone, frame));
    }

    /// <summary>
    /// Submits a command to the processing queue.
    /// Commands are dropped if the screen is disconnected.
    /// </summary>
    public void EnqueueCommand(ScreenCommand command) => _commandQueue.Enqueue(command);

    /// <summary>
    /// Connects to the screen (polling if not found), initializes, and starts
    /// the command queue and widget scheduler.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await ConnectAsync(_cts.Token);

        _commandQueue.Start(_cts.Token);
        _scheduler.Start(_cts.Token);

        Console.WriteLine("Widgets running.");
    }

    public async Task StopAsync()
    {
        if (_cts is null) return;

        await _cts.CancelAsync();
        await _scheduler.Stop();
        await _commandQueue.StopAsync();

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
                    await Console.Error.WriteLineAsync($"Failed to initialize screen on {port}: {ex.Message}");
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

        Console.Error.WriteLine("Screen disconnected. Stopping scheduler and scanning for reconnect...");

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
            await _scheduler.Stop();
            await ConnectAsync(ct);

            Console.WriteLine("Screen reconnected. Restarting scheduler.");
            _scheduler.Start(ct);
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
