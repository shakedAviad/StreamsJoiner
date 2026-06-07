using StreamsJoiner.Core.Models;
using StreamsJoiner.Messaging.Interfaces;

namespace StreamsJoiner.Messaging.Providers.Redis.Validation;

internal sealed class StreamEventValidator
{
    internal bool ValidateAgentEvent(RawStreamMessage message)
    {
        if (!HasNonEmptyCallId(message))
        {
            return false;
        }

        if (!message.Fields.TryGetValue("agentId", out string? agentId) || string.IsNullOrEmpty(agentId))
        {
            return false;
        }

        if (!message.Fields.TryGetValue("agentName", out string? agentName) || string.IsNullOrEmpty(agentName))
        {
            return false;
        }

        if (!message.Fields.TryGetValue("eventType", out string? eventType))
        {
            return false;
        }

        return Enum.TryParse<AgentEventType>(eventType, out _);
    }

    internal bool ValidateCustomerEvent(RawStreamMessage message)
    {
        if (!HasNonEmptyCallId(message))
        {
            return false;
        }

        if (!message.Fields.TryGetValue("customerId", out string? customerId) || string.IsNullOrEmpty(customerId))
        {
            return false;
        }

        if (!message.Fields.TryGetValue("phoneNumber", out string? phoneNumber) || string.IsNullOrEmpty(phoneNumber))
        {
            return false;
        }

        if (!message.Fields.TryGetValue("eventType", out string? eventType))
        {
            return false;
        }

        return Enum.TryParse<CustomerEventType>(eventType, out _);
    }

    internal bool ValidateBusinessDataEvent(RawStreamMessage message) =>
        HasNonEmptyCallId(message);

    private static bool HasNonEmptyCallId(RawStreamMessage message) =>
        message.Fields.TryGetValue("callId", out string? callId) && !string.IsNullOrWhiteSpace(callId);
}
