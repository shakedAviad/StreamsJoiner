using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Validation;

namespace StreamsJoiner.Messaging.Providers.Redis.Parsing;

internal sealed class BusinessDataEventParser(
    StreamEventValidator validator,
    ILogger<BusinessDataEventParser> logger)
{
    internal BusinessDataStreamEvent? Parse(RawStreamMessage message)
    {
        if (!validator.ValidateBusinessDataEvent(message))
        {
            logger.LogWarning("BusinessDataEventParser: invalid message {MessageId}", message.MessageId);
            return null;
        }

        message.Fields.TryGetValue("timestamp", out string? timestamp);

        Dictionary<string, string> data = [];
        foreach (KeyValuePair<string, string> entry in message.Fields)
        {
            if (entry.Key != "callId" && entry.Key != "timestamp")
            {
                data[entry.Key] = entry.Value;
            }
        }

        return new BusinessDataStreamEvent
        {
            CallId = message.Fields["callId"],
            Timestamp = timestamp ?? string.Empty,
            Data = data
        };
    }
}
