using StreamsJoiner.Core.Events;

namespace StreamsJoiner.Messaging.Interfaces;

public interface IConsumer
{
    Task ConsumeAsync(string streamName, Func<StreamEvent, CancellationToken, Task> onMessage, CancellationToken ct);
}
