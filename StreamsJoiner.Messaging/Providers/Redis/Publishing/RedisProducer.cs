using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Messaging.Interfaces;

namespace StreamsJoiner.Messaging.Providers.Redis.Publishing;

internal sealed class RedisProducer(
    IConnectionMultiplexer multiplexer,
    ILogger<RedisProducer> logger) : IProducer
{
    private const string OutputStreamName = "joined-call-events";

    public async Task ProduceAsync(CallEvent callEvent, CancellationToken ct)
    {
        IDatabase database = multiplexer.GetDatabase();
        Dictionary<string, string> map = callEvent.ToMap();

        NameValueEntry[] entries = new NameValueEntry[map.Count];
        int index = 0;
        foreach (KeyValuePair<string, string> pair in map)
        {
            entries[index++] = new NameValueEntry(pair.Key, pair.Value);
        }

        await database.StreamAddAsync(OutputStreamName, entries);
        logger.LogDebug("Produced CallEvent callId={CallId} status={Status}", callEvent.CallId, callEvent.Status);
    }
}
