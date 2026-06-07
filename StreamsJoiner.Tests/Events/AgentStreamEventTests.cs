using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;
using Xunit;

namespace StreamsJoiner.Tests.Events;

public sealed class AgentStreamEventTests
{
    private static AgentStreamEvent JoinEvent(string agentId, string agentName = "Alice") =>
        new()
        {
            CallId = "call-1",
            Timestamp = "ts",
            AgentId = agentId,
            AgentName = agentName,
            EventType = AgentEventType.AGENT_JOINED
        };

    private static AgentStreamEvent LeaveEvent(string agentId) =>
        new()
        {
            CallId = "call-1",
            Timestamp = "ts",
            AgentId = agentId,
            AgentName = "Alice",
            EventType = AgentEventType.AGENT_LEFT
        };

    [Fact]
    public void ExecuteEvent_FirstAgentJoins_ReturnsStartedAndSetsState()
    {
        CallState state = new();

        CallStatus? result = JoinEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.STARTED);
        state.ActiveAgents.Should().Contain("agent-1");
        state.LastAgentId.Should().Be("agent-1");
        state.LastAgentName.Should().Be("Alice");
    }

    [Fact]
    public void ExecuteEvent_AgentJoinsWhenCustomerPresent_ReturnsConnected()
    {
        CallState state = new();
        state.ActiveCustomers.Add("customer-1");
        state.CurrentStatus = CallStatus.STARTED;

        CallStatus? result = JoinEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.CONNECTED);
    }

    [Fact]
    public void ExecuteEvent_AgentLeaves_RemovedFromActiveAgents()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.CurrentStatus = CallStatus.STARTED;

        LeaveEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        state.ActiveAgents.Should().NotContain("agent-1");
    }

    [Fact]
    public void ExecuteEvent_AgentLeaves_LastAgentPersists()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.LastAgentId = "agent-1";
        state.LastAgentName = "Alice";
        state.CurrentStatus = CallStatus.STARTED;

        LeaveEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        state.LastAgentId.Should().Be("agent-1");
        state.LastAgentName.Should().Be("Alice");
    }

    [Fact]
    public void ExecuteEvent_AgentLeavesConnectedCall_ReturnsDisconnected()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.ActiveCustomers.Add("customer-1");
        state.CurrentStatus = CallStatus.CONNECTED;

        CallStatus? result = LeaveEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.DISCONNECTED);
    }

    [Fact]
    public void ExecuteEvent_LastParticipantLeaves_ReturnsEnded()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.CurrentStatus = CallStatus.STARTED;

        CallStatus? result = LeaveEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.ENDED);
    }

    [Fact]
    public void ExecuteEvent_DoubleJoin_ReturnsNullAndCountUnchanged()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.CurrentStatus = CallStatus.STARTED;

        CallStatus? result = JoinEvent("agent-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().BeNull();
        state.ActiveAgents.Should().HaveCount(1);
    }

    [Fact]
    public void ExecuteEvent_LeaveOfUnknownAgent_ReturnsNull()
    {
        CallState state = new();

        CallStatus? result = LeaveEvent("agent-unknown").ExecuteEvent(state, NullLogger.Instance);

        result.Should().BeNull();
    }
}
