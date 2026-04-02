using System.Threading.Channels;
using ScreenDriver.Controller.Events;
using ScreenDriver.Device;

namespace ScreenDriver.Controller.Commands;

/// <summary>
/// Serializes screen commands so concurrent producers cannot interleave bytes on the serial port.
/// Dispatches each command to the appropriate ScreenDevice method.
/// Signals disconnect to the controller when a serial exception occurs.
/// </summary>
public sealed class ScreenCommandQueue
{
    private readonly Func<ScreenDevice?> _getDevice;
    private readonly EventBus _bus;
    private readonly Channel<ScreenCommand> _channel = Channel.CreateUnbounded<ScreenCommand>();
    private Task? _drainTask;

    /// <summary>
    /// Raised when a serial exception indicates the device has disconnected.
    /// </summary>
    public event Action? Disconnected;

    public ScreenCommandQueue(Func<ScreenDevice?> getDevice, EventBus bus)
    {
        _getDevice = getDevice;
        _bus = bus;
    }

    public void Enqueue(ScreenCommand command)
    {
        _channel.Writer.TryWrite(command);
    }

    public void Start(CancellationToken ct)
    {
        _drainTask = Task.Run(() => DrainAsync(ct), ct);
    }

    public async Task StopAsync()
    {
        _channel.Writer.Complete();

        if (_drainTask is not null)
            await _drainTask;
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var command in _channel.Reader.ReadAllAsync(ct))
            {
                var device = _getDevice();
                if (device is null)
                    continue;

                try
                {
                    Dispatch(command, device, _bus);
                }
                catch (Exception ex) when (ex is IOException or TimeoutException or ObjectDisposedException
                                               or InvalidOperationException)
                {
                    _bus.Publish(new Error("CommandQueue", ex));
                    Disconnected?.Invoke();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    private static void Dispatch(ScreenCommand command, ScreenDevice device, EventBus bus)
    {
        command.Execute(device);
        if (command is NotifiableCommand notifiableCommand)
            bus.Publish(notifiableCommand.Event);
    }
}