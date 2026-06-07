using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Validation;

namespace StreamsJoiner.Messaging.Providers.Redis.Parsing;

internal sealed class AgentEventParser(
    StreamEventValidator validator,
    ILogger<AgentEventParser> logger)
{
    internal AgentStreamEvent? Parse(RawStreamMessage message)
    {
        if (!validator.ValidateAgentEvent(message))
        {
            logger.LogWarning("AgentEventParser: invalid message {MessageId}", message.MessageId);
            return null;
        }

        if (!message.Fields.TryGetValue("agentId", out string? agentId) || string.IsNullOrEmpty(agentId))
        {
            logger.LogWarning("AgentEventParser: missing agentId in message {MessageId}", message.MessageId);
            return null;
        }

        if (!message.Fields.TryGetValue("agentName", out string? agentName) || string.IsNullOrEmpty(agentName))
        {
            logger.LogWarning("AgentEventParser: missing agentName in message {MessageId}", message.MessageId);
            return null;
        }

        if (!message.Fields.TryGetValue("eventType", out string? eventTypeStr) ||
            !Enum.TryParse<AgentEventType>(eventTypeStr, out AgentEventType eventType))
        {
            logger.LogWarning("AgentEventParser: unparseable eventType in message {MessageId}", message.MessageId);
            return null;
        }

        message.Fields.TryGetValue("timestamp", out string? timestamp);

        return new AgentStreamEvent
        {
            CallId = message.Fields["callId"],
            AgentId = agentId,
            AgentName = agentName,
            EventType = eventType,
            Timestamp = timestamp ?? string.Empty
        };
    }
}
