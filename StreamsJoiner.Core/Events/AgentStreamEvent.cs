using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;
using StreamsJoiner.Core.StateMachine;

namespace StreamsJoiner.Core.Events;

public sealed class AgentStreamEvent : StreamEvent
{
    public string AgentId { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public AgentEventType EventType { get; init; }

    public override CallStatus? ExecuteEvent(CallState state, ILogger logger)
    {
        if (EventType == AgentEventType.AGENT_JOINED)
        {
            if (state.ActiveAgents.Contains(AgentId))
            {
                return null;
            }
            state.ActiveAgents.Add(AgentId);
            state.LastAgentId = AgentId;
            state.LastAgentName = AgentName;
        }
        else
        {
            if (!state.ActiveAgents.Contains(AgentId))
            {
                logger.LogWarning("Leave event for unknown agent {AgentId} on call {CallId}", AgentId, CallId);
                return null;
            }
            state.ActiveAgents.Remove(AgentId);
        }

        CallStatusComputer computer = new();
        CallStatus? newStatus = computer.Compute(
            state.ActiveAgents.Count,
            state.ActiveCustomers.Count,
            state.CurrentStatus);

        if (newStatus is { } status)
        {
            state.CurrentStatus = status;
        }

        return newStatus;
    }
}
