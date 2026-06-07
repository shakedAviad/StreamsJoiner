namespace StreamsJoiner.Core.Models;

/// <summary>
/// Represents a joined call lifecycle event published to the "joined-call-events" output stream.
/// </summary>
public class CallEvent
{
    public string? CallId { get; set; }
    public CallStatus Status { get; set; }
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? CustomerId { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// All accumulated business data key-value pairs for this call.
    /// Keys are arbitrary business field names (e.g., "skillName", "priority", "queueId").
    /// </summary>
    public Dictionary<string, string>? BusinessData { get; set; }

    public string? Timestamp { get; set; }

    /// <summary>
    /// Converts this event to a flat map for publishing to a Redis Stream.
    /// Business data keys are namespaced with a "data." prefix.
    /// </summary>
    public Dictionary<string, string> ToMap()
    {
        Dictionary<string, string> map = [];
        if (CallId is not null) map["callId"] = CallId;
        map["status"] = Status.ToString();
        if (AgentId is not null) map["agentId"] = AgentId;
        if (AgentName is not null) map["agentName"] = AgentName;
        if (CustomerId is not null) map["customerId"] = CustomerId;
        if (PhoneNumber is not null) map["phoneNumber"] = PhoneNumber;
        if (BusinessData is not null && BusinessData.Count > 0)
        {
            foreach (KeyValuePair<string, string> entry in BusinessData)
            {
                map["data." + entry.Key] = entry.Value;
            }
        }
        if (Timestamp is not null) map["timestamp"] = Timestamp;
        return map;
    }

    public override string ToString() =>
        $"CallEvent{{callId='{CallId}', status='{Status}', agentId='{AgentId}', customerId='{CustomerId}', timestamp='{Timestamp}'}}";
}
