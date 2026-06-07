using StreamsJoiner.Messaging.Interfaces;

namespace StreamsJoiner.Messaging.Providers.Redis.Validation;

internal sealed class StreamEventValidator
{
    internal bool ValidateAgentEvent(RawStreamMessage message) =>
        HasNonEmptyCallId(message);

    internal bool ValidateCustomerEvent(RawStreamMessage message) =>
        HasNonEmptyCallId(message);

    internal bool ValidateBusinessDataEvent(RawStreamMessage message) =>
        HasNonEmptyCallId(message);

    private static bool HasNonEmptyCallId(RawStreamMessage message) =>
        message.Fields.TryGetValue("callId", out string? callId) && !string.IsNullOrWhiteSpace(callId);
}
