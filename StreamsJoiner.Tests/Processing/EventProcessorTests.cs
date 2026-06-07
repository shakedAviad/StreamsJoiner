using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.Processing;
using StreamsJoiner.Core.State;
using Xunit;

namespace StreamsJoiner.Tests.Processing;

public sealed class EventProcessorTests
{
    private static AgentStreamEvent AgentJoined(string callId, string agentId = "agent-1", string agentName = "Alice") =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            AgentId = agentId,
            AgentName = agentName,
            EventType = AgentEventType.AGENT_JOINED
        };

    private static AgentStreamEvent AgentLeft(string callId, string agentId = "agent-1", string agentName = "Alice") =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            AgentId = agentId,
            AgentName = agentName,
            EventType = AgentEventType.AGENT_LEFT
        };

    private static CustomerStreamEvent CustomerJoined(string callId, string customerId = "cust-1", string phone = "555-0001") =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            CustomerId = customerId,
            PhoneNumber = phone,
            EventType = CustomerEventType.CUSTOMER_JOINED
        };

    private static CustomerStreamEvent CustomerLeft(string callId, string customerId = "cust-1", string phone = "555-0001") =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            CustomerId = customerId,
            PhoneNumber = phone,
            EventType = CustomerEventType.CUSTOMER_LEFT
        };

    private static BusinessDataStreamEvent BusinessData(string callId, string key, string value) =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            Data = new Dictionary<string, string> { [key] = value }
        };

    private static EventProcessor CreateProcessor(
        List<CallEvent> published,
        string? removedCallId = null,
        Action<string>? removeCallback = null,
        TimeSpan? timeout = null)
    {
        Action<string> remove = removeCallback ?? (_ => { });
        return new EventProcessor(
            NullLogger<EventProcessor>.Instance,
            remove,
            (callEvent, _) =>
            {
                published.Add(callEvent);
                return Task.CompletedTask;
            },
            timeout);
    }

    [Fact]
    public async Task ProcessAsync_HappyPath_PublishesFourStatusesInOrder()
    {
        List<CallEvent> published = [];
        EventProcessor processor = CreateProcessor(published);
        CallActor actor = new();

        await actor.EventChannel.Writer.WriteAsync(CustomerJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(AgentLeft("call-1"));
        await actor.EventChannel.Writer.WriteAsync(CustomerLeft("call-1"));
        actor.EventChannel.Writer.Complete();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().HaveCount(4);
        published[0].Status.Should().Be(CallStatus.STARTED);
        published[1].Status.Should().Be(CallStatus.CONNECTED);
        published[2].Status.Should().Be(CallStatus.DISCONNECTED);
        published[3].Status.Should().Be(CallStatus.ENDED);

        foreach (CallEvent callEvent in published)
        {
            callEvent.BusinessData.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ProcessAsync_AgentTransfer_PublishesSixStatuses()
    {
        List<CallEvent> published = [];
        EventProcessor processor = CreateProcessor(published);
        CallActor actor = new();

        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1", "agent-1", "Alice"));
        await actor.EventChannel.Writer.WriteAsync(CustomerJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(AgentLeft("call-1", "agent-1", "Alice"));
        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1", "agent-2", "Bob"));
        await actor.EventChannel.Writer.WriteAsync(AgentLeft("call-1", "agent-2", "Bob"));
        await actor.EventChannel.Writer.WriteAsync(CustomerLeft("call-1"));
        actor.EventChannel.Writer.Complete();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().HaveCount(6);
        published[0].Status.Should().Be(CallStatus.STARTED);
        published[1].Status.Should().Be(CallStatus.CONNECTED);
        published[2].Status.Should().Be(CallStatus.DISCONNECTED);
        published[3].Status.Should().Be(CallStatus.CONNECTED);
        published[4].Status.Should().Be(CallStatus.DISCONNECTED);
        published[5].Status.Should().Be(CallStatus.ENDED);

        published[2].AgentId.Should().Be("agent-1");
        published[4].AgentId.Should().Be("agent-2");
    }

    [Fact]
    public async Task ProcessAsync_LeaveBeforeJoin_SkipsUnknownAgentAndPublishesTwoStatuses()
    {
        List<CallEvent> published = [];
        EventProcessor processor = CreateProcessor(published);
        CallActor actor = new();

        await actor.EventChannel.Writer.WriteAsync(AgentLeft("call-1"));
        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(CustomerJoined("call-1"));
        actor.EventChannel.Writer.Complete();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().HaveCount(2);
        published[0].Status.Should().Be(CallStatus.STARTED);
        published[1].Status.Should().Be(CallStatus.CONNECTED);
    }

    [Fact]
    public async Task ProcessAsync_LateBusinessData_DisconnectedEventContainsBusinessData()
    {
        List<CallEvent> published = [];
        EventProcessor processor = CreateProcessor(published);
        CallActor actor = new();

        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(CustomerJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(BusinessData("call-1", "skill", "support"));
        await actor.EventChannel.Writer.WriteAsync(CustomerLeft("call-1"));
        actor.EventChannel.Writer.Complete();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().HaveCount(3);
        published[0].Status.Should().Be(CallStatus.STARTED);
        published[1].Status.Should().Be(CallStatus.CONNECTED);
        published[2].Status.Should().Be(CallStatus.DISCONNECTED);
        published[2].BusinessData.Should().ContainKey("skill").WhoseValue.Should().Be("support");
    }

    [Fact]
    public async Task ProcessAsync_EndedGuard_ExtraEventAfterEndedIsNotProcessed()
    {
        List<CallEvent> published = [];
        EventProcessor processor = CreateProcessor(published);
        CallActor actor = new();

        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(CustomerJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(AgentLeft("call-1"));
        await actor.EventChannel.Writer.WriteAsync(CustomerLeft("call-1"));
        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1"));
        actor.EventChannel.Writer.Complete();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().HaveCount(4);
        published[3].Status.Should().Be(CallStatus.ENDED);
    }

    [Fact]
    public async Task ProcessAsync_InactivityTimeout_ExitsWithoutPublishAndCallsRemoveFromActive()
    {
        List<CallEvent> published = [];
        string? removedCallId = null;
        EventProcessor processor = new EventProcessor(
            NullLogger<EventProcessor>.Instance,
            callId => removedCallId = callId,
            (callEvent, _) =>
            {
                published.Add(callEvent);
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(100));
        CallActor actor = new();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().BeEmpty();
        removedCallId.Should().Be("call-1");
    }

    [Fact]
    public async Task ProcessAsync_BusinessDataSnapshotIsCopy_ModifyingOneDoesNotAffectOther()
    {
        List<CallEvent> published = [];
        EventProcessor processor = CreateProcessor(published);
        CallActor actor = new();

        await actor.EventChannel.Writer.WriteAsync(AgentJoined("call-1"));
        await actor.EventChannel.Writer.WriteAsync(BusinessData("call-1", "k", "v"));
        await actor.EventChannel.Writer.WriteAsync(CustomerJoined("call-1"));
        actor.EventChannel.Writer.Complete();

        await processor.ProcessAsync("call-1", actor, CancellationToken.None);

        published.Should().HaveCount(2);
        published[0].Status.Should().Be(CallStatus.STARTED);
        published[0].BusinessData.Should().NotContainKey("k");
        published[1].Status.Should().Be(CallStatus.CONNECTED);
        published[1].BusinessData.Should().ContainKey("k").WhoseValue.Should().Be("v");

        published[0].BusinessData!["extra"] = "val";
        published[1].BusinessData.Should().NotContainKey("extra");
    }
}
