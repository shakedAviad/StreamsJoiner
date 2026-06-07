namespace StreamsJoiner.Core.Models;

/// <summary>
/// Represents an event from the "agent-events" stream.
/// Agents join or leave calls.
/// </summary>
public class AgentEvent
{
    public string? CallId { get; set; }
    public AgentEventType EventType { get; set; }
    public string? AgentId { get; set; }
    public string? AgentName { get; set; }
    public string? Timestamp { get; set; }

    public AgentEvent() { }

    public AgentEvent(string? callId, AgentEventType eventType, string? agentId, string? agentName, string? timestamp)
    {
        CallId = callId;
        EventType = eventType;
        AgentId = agentId;
        AgentName = agentName;
        Timestamp = timestamp;
    }

    public override string ToString() =>
        $"AgentEvent{{callId='{CallId}', eventType='{EventType}', agentId='{AgentId}', agentName='{AgentName}', timestamp='{Timestamp}'}}";
}
