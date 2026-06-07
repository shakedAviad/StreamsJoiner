using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Parsing;

namespace StreamsJoiner.Messaging.Providers.Redis.Reading;

internal sealed class RedisConsumer(
    IConnectionMultiplexer multiplexer,
    AgentEventParser agentEventParser,
    CustomerEventParser customerEventParser,
    BusinessDataEventParser businessDataEventParser,
    ILogger<RedisConsumer> logger,
    TimeSpan? pollingInterval = null) : IConsumer
{
    private readonly TimeSpan _pollingInterval = pollingInterval ?? TimeSpan.FromMilliseconds(100);

    public async Task ConsumeAsync(string streamName, Func<StreamEvent, CancellationToken, Task> onMessage, CancellationToken ct)
    {
        IDatabase database = multiplexer.GetDatabase();
        string lastId = "0";

        while (!ct.IsCancellationRequested)
        {
            StreamEntry[] entries = await database.StreamReadAsync(streamName, lastId, 100);

            if (entries.Length == 0)
            {
                await Task.Delay(_pollingInterval, ct);
            }
            else
            {
                foreach (StreamEntry entry in entries)
                {
                    RawStreamMessage rawMessage = ToRawStreamMessage(entry);
                    StreamEvent? streamEvent = SelectParser(streamName, rawMessage);

                    if (streamEvent is { } validEvent)
                    {
                        await onMessage(validEvent, ct);
                        lastId = entry.Id.ToString();
                    }
                }
            }
        }
    }

    private StreamEvent? SelectParser(string streamName, RawStreamMessage rawMessage) =>
        streamName switch
        {
            "agent-events" => agentEventParser.Parse(rawMessage),
            "customer-events" => customerEventParser.Parse(rawMessage),
            "business-data-events" => businessDataEventParser.Parse(rawMessage),
            _ => LogUnknownStream(streamName, rawMessage.MessageId)
        };

    private StreamEvent? LogUnknownStream(string streamName, string messageId)
    {
        logger.LogWarning("RedisConsumer: unknown stream {StreamName}, messageId {MessageId}", streamName, messageId);
        return null;
    }

    private static RawStreamMessage ToRawStreamMessage(StreamEntry entry)
    {
        Dictionary<string, string> fields = [];
        foreach (NameValueEntry nameValue in entry.Values)
        {
            fields[nameValue.Name.ToString()] = nameValue.Value.ToString() ?? string.Empty;
        }
        return new RawStreamMessage(entry.Id.ToString(), fields);
    }
}
