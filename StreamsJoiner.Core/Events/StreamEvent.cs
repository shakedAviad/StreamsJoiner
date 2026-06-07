using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;

namespace StreamsJoiner.Core.Events;

public abstract class StreamEvent
{
    public string CallId { get; init; } = string.Empty;
    public string Timestamp { get; init; } = string.Empty;

    public abstract CallStatus? ExecuteEvent(CallState state, ILogger logger);
}
