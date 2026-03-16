using System.Threading.Channels;

namespace ScreenDriver.Queue;

/// <summary>
/// Serializes all screen writes so concurrent widget timers cannot interleave bytes on the serial port.
/// </summary>
public sealed class ScreenWriteQueue
{
    private readonly ScreenDevice _device;
    private readonly Channel<WriteRequest> _channel = Channel.CreateUnbounded<WriteRequest>();
    private Task? _drainTask;

    public ScreenWriteQueue(ScreenDevice device)
    {
        _device = device;
    }

    public void Enqueue(WriteRequest request)
    {
        _channel.Writer.TryWrite(request);
    }

    public void StartAsync(CancellationToken ct)
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
            await foreach (var request in _channel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    _device.DisplayBitmap(
                        request.Zone.X,
                        request.Zone.Y,
                        request.Zone.EndX,
                        request.Zone.EndY,
                        request.Frame.Data);
                }
                catch (TimeoutException ex)
                {
                    await Console.Error.WriteLineAsync($"Write failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Write queue stopped");
        }
    }
}
