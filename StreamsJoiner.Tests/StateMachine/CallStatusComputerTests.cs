using FluentAssertions;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.StateMachine;
using Xunit;

namespace StreamsJoiner.Tests.StateMachine;

public sealed class CallStatusComputerTests
{
    private readonly CallStatusComputer _computer = new();

    [Fact]
    public void Compute_FirstAgentJoins_ReturnsStarted()
    {
        CallStatus? result = _computer.Compute(1, 0, null);

        result.Should().Be(CallStatus.STARTED);
    }

    [Fact]
    public void Compute_FirstCustomerJoins_ReturnsStarted()
    {
        CallStatus? result = _computer.Compute(0, 1, null);

        result.Should().Be(CallStatus.STARTED);
    }

    [Fact]
    public void Compute_BothSidesJoinFromStarted_ReturnsConnected()
    {
        CallStatus? result = _computer.Compute(1, 1, CallStatus.STARTED);

        result.Should().Be(CallStatus.CONNECTED);
    }

    [Fact]
    public void Compute_AgentJoinsAfterDisconnected_ReturnsConnected()
    {
        CallStatus? result = _computer.Compute(1, 1, CallStatus.DISCONNECTED);

        result.Should().Be(CallStatus.CONNECTED);
    }

    [Fact]
    public void Compute_CustomerLeavesAgentRemains_ReturnsDisconnected()
    {
        CallStatus? result = _computer.Compute(1, 0, CallStatus.CONNECTED);

        result.Should().Be(CallStatus.DISCONNECTED);
    }

    [Fact]
    public void Compute_AgentLeavesCustomerRemains_ReturnsDisconnected()
    {
        CallStatus? result = _computer.Compute(0, 1, CallStatus.CONNECTED);

        result.Should().Be(CallStatus.DISCONNECTED);
    }

    [Fact]
    public void Compute_AllLeaveFromConnected_ReturnsEnded()
    {
        CallStatus? result = _computer.Compute(0, 0, CallStatus.CONNECTED);

        result.Should().Be(CallStatus.ENDED);
    }

    [Fact]
    public void Compute_AllLeaveFromDisconnected_ReturnsEnded()
    {
        CallStatus? result = _computer.Compute(0, 0, CallStatus.DISCONNECTED);

        result.Should().Be(CallStatus.ENDED);
    }

    [Fact]
    public void Compute_AlreadyConnected_ReturnsNull()
    {
        CallStatus? result = _computer.Compute(1, 1, CallStatus.CONNECTED);

        result.Should().BeNull();
    }

    [Fact]
    public void Compute_AlreadyStartedWithOneAgent_ReturnsNull()
    {
        CallStatus? result = _computer.Compute(1, 0, CallStatus.STARTED);

        result.Should().BeNull();
    }

    [Fact]
    public void Compute_AlreadyDisconnected_ReturnsNull()
    {
        CallStatus? result = _computer.Compute(0, 1, CallStatus.DISCONNECTED);

        result.Should().BeNull();
    }

    [Fact]
    public void Compute_NothingHasHappened_ReturnsNull()
    {
        CallStatus? result = _computer.Compute(0, 0, null);

        result.Should().BeNull();
    }
}
