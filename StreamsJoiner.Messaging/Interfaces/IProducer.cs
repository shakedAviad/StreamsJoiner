using StreamsJoiner.Core.Models;

namespace StreamsJoiner.Messaging.Interfaces;

public interface IProducer
{
    Task ProduceAsync(CallEvent callEvent, CancellationToken ct);
}
