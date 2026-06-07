using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;

namespace StreamsJoiner.Core.Events;

public sealed class BusinessDataStreamEvent : StreamEvent
{
    public Dictionary<string, string> Data { get; init; } = [];

    public override CallStatus? ExecuteEvent(CallState state, ILogger logger)
    {
        foreach (KeyValuePair<string, string> entry in Data)
        {
            state.AccumulatedBusinessData[entry.Key] = entry.Value;
        }
        return null;
    }
}
