using System.Threading.Channels;

namespace ScreenDriver.Controller.Events;

public sealed class EventBus
{
    private readonly Channel<Event> _channel = Channel.CreateUnbounded<Event>();

    public void Publish(Event e) => _channel.Writer.TryWrite(e);

    public IAsyncEnumerable<Event> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
