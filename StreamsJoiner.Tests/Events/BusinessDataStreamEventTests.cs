using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;
using Xunit;

namespace StreamsJoiner.Tests.Events;

public sealed class BusinessDataStreamEventTests
{
    private static BusinessDataStreamEvent CreateEvent(Dictionary<string, string> data) =>
        new()
        {
            CallId = "call-1",
            Timestamp = "ts",
            Data = data
        };

    [Fact]
    public void ExecuteEvent_KeysMergedIntoAccumulatedBusinessData()
    {
        CallState state = new();

        CreateEvent(new() { ["skillName"] = "support" }).ExecuteEvent(state, NullLogger.Instance);

        state.AccumulatedBusinessData.Should().ContainKey("skillName")
            .WhoseValue.Should().Be("support");
    }

    [Fact]
    public void ExecuteEvent_SameKeyLastWins()
    {
        CallState state = new();
        CreateEvent(new() { ["priority"] = "low" }).ExecuteEvent(state, NullLogger.Instance);
        CreateEvent(new() { ["priority"] = "high" }).ExecuteEvent(state, NullLogger.Instance);

        state.AccumulatedBusinessData["priority"].Should().Be("high");
    }

    [Fact]
    public void ExecuteEvent_AlwaysReturnsNull()
    {
        CallState state = new();

        CallStatus? result = CreateEvent(new() { ["key"] = "value" }).ExecuteEvent(state, NullLogger.Instance);

        result.Should().BeNull();
    }

    [Fact]
    public void ExecuteEvent_MultipleKeysMergedAll()
    {
        CallState state = new();
        Dictionary<string, string> data = new()
        {
            ["skillName"] = "support",
            ["priority"] = "high",
            ["queueId"] = "q1"
        };

        CreateEvent(data).ExecuteEvent(state, NullLogger.Instance);

        state.AccumulatedBusinessData.Should().ContainKey("skillName");
        state.AccumulatedBusinessData.Should().ContainKey("priority");
        state.AccumulatedBusinessData.Should().ContainKey("queueId");
    }

    [Fact]
    public void ExecuteEvent_DoesNotAffectCurrentStatus()
    {
        CallState state = new();
        state.CurrentStatus = CallStatus.CONNECTED;

        CreateEvent(new() { ["key"] = "value" }).ExecuteEvent(state, NullLogger.Instance);

        state.CurrentStatus.Should().Be(CallStatus.CONNECTED);
    }
}
