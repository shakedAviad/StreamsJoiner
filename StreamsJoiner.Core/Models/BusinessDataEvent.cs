namespace StreamsJoiner.Core.Models;

/// <summary>
/// Represents an event from the "business-data-events" stream.
/// <para>
/// Contains arbitrary key-value business context data related to a call.
/// Any field in the Redis Stream message other than <c>callId</c> and <c>timestamp</c>
/// is treated as a business data key-value pair.
/// </para>
/// Examples of keys: <c>skillName</c>, <c>priority</c>, <c>queueId</c>, <c>language</c>, etc.
/// </summary>
public class BusinessDataEvent
{
    public string? CallId { get; set; }
    public string? Timestamp { get; set; }

    /// <summary>
    /// The business data key-value pairs.
    /// Keys are arbitrary business field names (e.g., "skillName", "priority", "queueId").
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = new();

    public BusinessDataEvent() { }

    public BusinessDataEvent(string? callId, string? timestamp, Dictionary<string, string>? data)
    {
        CallId = callId;
        Timestamp = timestamp;
        Data = data is not null ? new Dictionary<string, string>(data) : new Dictionary<string, string>();
    }

    public override string ToString() =>
        $"BusinessDataEvent{{callId='{CallId}', data=[{string.Join(", ", Data.Select(kv => $"{kv.Key}={kv.Value}"))}], timestamp='{Timestamp}'}}";
}
