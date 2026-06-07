using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Validation;

namespace StreamsJoiner.Messaging.Providers.Redis.Parsing;

internal sealed class CustomerEventParser(
    StreamEventValidator validator,
    ILogger<CustomerEventParser> logger)
{
    internal CustomerStreamEvent? Parse(RawStreamMessage message)
    {
        if (!validator.ValidateCustomerEvent(message))
        {
            logger.LogWarning("CustomerEventParser: invalid message {MessageId}", message.MessageId);
            return null;
        }

        if (!message.Fields.TryGetValue("customerId", out string? customerId) || string.IsNullOrEmpty(customerId))
        {
            logger.LogWarning("CustomerEventParser: missing customerId in message {MessageId}", message.MessageId);
            return null;
        }

        if (!message.Fields.TryGetValue("phoneNumber", out string? phoneNumber) || string.IsNullOrEmpty(phoneNumber))
        {
            logger.LogWarning("CustomerEventParser: missing phoneNumber in message {MessageId}", message.MessageId);
            return null;
        }

        if (!message.Fields.TryGetValue("eventType", out string? eventTypeStr) ||
            !Enum.TryParse<CustomerEventType>(eventTypeStr, out CustomerEventType eventType))
        {
            logger.LogWarning("CustomerEventParser: unparseable eventType in message {MessageId}", message.MessageId);
            return null;
        }

        message.Fields.TryGetValue("timestamp", out string? timestamp);

        return new CustomerStreamEvent
        {
            CallId = message.Fields["callId"],
            CustomerId = customerId,
            PhoneNumber = phoneNumber,
            EventType = eventType,
            Timestamp = timestamp ?? string.Empty
        };
    }
}
