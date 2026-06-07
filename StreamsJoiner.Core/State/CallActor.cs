using System.Threading.Channels;
using StreamsJoiner.Core.Events;

namespace StreamsJoiner.Core.State;

public sealed class CallActor
{
    public Channel<StreamEvent> EventChannel { get; }
    public CallState State { get; }
    public Task? ProcessorTask { get; set; }
    public DateTime CreatedAt { get; }

    public CallActor()
    {
        EventChannel = Channel.CreateUnbounded<StreamEvent>();
        State = new();
        CreatedAt = DateTime.UtcNow;
    }
}
