using ScreenDriver.Controller.Commands;
using ScreenDriver.Controller.Events;
using ScreenDriver.Device;
using ScreenDriver.Themes;

namespace ScreenDriver.Controller;

/// <summary>
/// Top-level coordinator that owns device lifecycle, command queue, and widget scheduling.
/// Handles disconnect detection (via queue event) and auto-reconnect via DeviceScanner.
/// External code can submit commands via EnqueueCommand.
/// </summary>
public sealed class ScreenController : IAsyncDisposable
{
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(5);

    private readonly string? _fixedPort;
    private readonly ThemeRegistry _registry;
    private readonly EventBus _bus;
    private readonly ScreenCommandQueue _commandQueue;
    private WidgetScheduler _scheduler;
    private CancellationTokenSource? _cts;
    private ScreenDevice? _device;
    private volatile bool _reconnecting;
    private string _activeTheme;
    private ScreenOrientation _activeOrientation = ScreenOrientation.Landscape;

    public string ActiveTheme => _activeTheme;

    public ScreenController(ThemeRegistry registry, EventBus bus, string? port = null)
    {
        _fixedPort = port;
        _registry = registry;
        _activeTheme = registry.DefaultThemeName;
        _bus = bus;
        _commandQueue = new ScreenCommandQueue(() => _device, bus);
        _commandQueue.Disconnected += OnDisconnect;
        _scheduler = CreateScheduler(registry.BuildDefault());
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

        await Connect(_cts.Token);

        _commandQueue.Start(_cts.Token);
        _scheduler.Start(_cts.Token);

        _bus.Publish(new Info("Controller", "Widgets running."));
    }

    public async Task Stop()
    {
        if (_cts is null) return;

        await _cts.CancelAsync();
        await _scheduler.Stop();
        await _commandQueue.StopAsync();

        if (_device is not null)
        {
            try
            {
                _device.ScreenOff();
            }
            catch
            {
                /* device may already be gone */
            }

            _device.Dispose();
            _device = null;
        }

        _cts.Dispose();
        _cts = null;
    }

    public async ValueTask DisposeAsync() => await Stop();

    /// <summary>
    /// Changes the screen orientation. If the active theme doesn't support the
    /// target layout mode, publishes an error and fills the screen black.
    /// Otherwise stops the scheduler, sends the orientation command, rebuilds
    /// the theme, and restarts the scheduler.
    /// </summary>
    public async Task SetOrientation(ScreenOrientation orientation)
    {
        if (_cts is null) return;

        if (orientation == _activeOrientation)
        {
            _bus.Publish(new Info("Controller", $"Already in {orientation} orientation."));
            return;
        }

        var layout = ScreenDevice.LayoutModeFor(orientation);

        // Scheduler should be stopped before changing orientation, screen should be cleared
        await _scheduler.Stop();
        EnqueueCommand(new ClearScreenCommand());

        if (!_registry.GetThemesForLayout(layout).Contains(_activeTheme))
        {
            _bus.Publish(new Error("Controller",
                new InvalidOperationException($"No '{_activeTheme}' theme available for {layout}")));
            EnqueueCommand(new FillScreenCommand(255, 0, 0));
            return;
        }

        EnqueueCommand(new SetOrientationCommand(orientation));
       

        _scheduler = CreateScheduler(_registry.Build(_activeTheme));
        _scheduler.Start(_cts.Token);
        _activeOrientation = orientation;

        _bus.Publish(new Info("Controller", $"Orientation changed to {orientation}."));
    }

    /// <summary>
    /// Hot-swaps the active theme. Stops the scheduler, builds the new theme,
    /// clears the screen, and restarts the scheduler. Device connection stays alive.
    /// </summary>
    public async Task SwapTheme(string name)
    {
        if (_cts is null) return;

        Theme theme;
        try
        {
            theme = _registry.Build(name);
        }
        catch (ArgumentException ex)
        {
            _bus.Publish(new Error("Controller", ex));
            return;
        }

        await _scheduler.Stop();

        EnqueueCommand(new FillScreenCommand(0, 0, 0));
        _scheduler = CreateScheduler(theme);
        _scheduler.Start(_cts.Token);
        _activeTheme = name;

        _bus.Publish(new Info("Controller", $"Theme switched to '{name}'."));
    }

    private WidgetScheduler CreateScheduler(Theme theme)
    {
        var scheduler = new WidgetScheduler(theme.Widgets, _bus);
        scheduler.FrameRendered += (zone, frame) =>
            EnqueueCommand(new DisplayBitmapCommand(zone, frame));
        return scheduler;
    }

    /// <summary>
    /// Polls for the screen until found, then opens and initializes it.
    /// </summary>
    private async Task Connect(CancellationToken ct)
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
                    _bus.Publish(new Info("Controller", $"Screen connected on {port} (size ID: 0x{sizeId:X2})"));

                    device.SetBrightness(0);
                    device.SetOrientation(ScreenOrientation.Landscape);
                    device.FillScreen(0, 0, 0);

                    _device = device;
                    // Success! Stop polling and return
                    return;
                }
                catch (Exception ex)
                {
                    _bus.Publish(new Error("Controller", ex));
                }
            }

            _bus.Publish(new Info("Controller", "Screen not found, retrying..."));
            await Task.Delay(ReconnectInterval, ct);
        }
    }

    private void OnDisconnect()
    {
        if (_reconnecting) return;
        _reconnecting = true;

        _bus.Publish(new Warning("Controller", "Screen disconnected. Reconnecting..."));

        // Dispose old device
        try
        {
            _device?.Dispose();
        }
        catch
        {
            /* already gone */
        }

        _device = null;

        // Fire-and-forget reconnect loop
        _ = Reconnect(_cts?.Token ?? CancellationToken.None);
    }

    private async Task Reconnect(CancellationToken ct)
    {
        try
        {
            await _scheduler.Stop();
            // Loops until the screen is connected or the app shutdown
            await Connect(ct);

            _bus.Publish(new Info("Controller", "Screen reconnected. Restarting scheduler."));
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