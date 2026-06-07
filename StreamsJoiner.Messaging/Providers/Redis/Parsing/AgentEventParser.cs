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

        if (!Enum.TryParse<AgentEventType>(message.Fields["eventType"], out AgentEventType eventType))
        {
            logger.LogWarning("AgentEventParser: unparseable eventType in message {MessageId}", message.MessageId);
            return null;
        }

        message.Fields.TryGetValue("timestamp", out string? timestamp);

        return new AgentStreamEvent
        {
            CallId = message.Fields["callId"],
            AgentId = message.Fields["agentId"],
            AgentName = message.Fields["agentName"],
            EventType = eventType,
            Timestamp = timestamp ?? string.Empty
        };
    }
}
