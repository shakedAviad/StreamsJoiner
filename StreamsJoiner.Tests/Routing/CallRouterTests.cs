using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.Routing;
using StreamsJoiner.Core.State;
using Xunit;

namespace StreamsJoiner.Tests.Routing;

public sealed class CallRouterTests
{
    private bool _processorStarted;

    private CallRouter CreateRouter()
    {
        _processorStarted = false;
        return new CallRouter(
            NullLogger<CallRouter>.Instance,
            (_, _, _) =>
            {
                _processorStarted = true;
                return Task.CompletedTask;
            });
    }

    private static AgentStreamEvent AgentJoinedEvent(string callId) =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            AgentId = "agent-1",
            AgentName = "Alice",
            EventType = AgentEventType.AGENT_JOINED
        };

    private static AgentStreamEvent AgentLeftEvent(string callId) =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            AgentId = "agent-1",
            AgentName = "Alice",
            EventType = AgentEventType.AGENT_LEFT
        };

    private static BusinessDataStreamEvent BusinessDataEvent(string callId, string key = "key", string value = "value") =>
        new()
        {
            CallId = callId,
            Timestamp = "ts",
            Data = new() { [key] = value }
        };

    [Fact]
    public async Task Route_ActiveCall_WritesEventToChannel()
    {
        CallRouter router = CreateRouter();
        await router.Route(AgentJoinedEvent("call-1"), CancellationToken.None);
        router.ActiveCalls["call-1"].EventChannel.Reader.TryRead(out _); // drain join event

        await router.Route(AgentLeftEvent("call-1"), CancellationToken.None);

        router.ActiveCalls["call-1"].EventChannel.Reader.TryRead(out StreamEvent? ev).Should().BeTrue();
        ev.Should().BeOfType<AgentStreamEvent>();
        router.PendingCalls.Should().NotContainKey("call-1");
    }

    [Fact]
    public async Task Route_StartEventToPendingCall_PromotesToActiveAndStartsProcessor()
    {
        CallRouter router = CreateRouter();
        await router.Route(AgentLeftEvent("call-1"), CancellationToken.None); // creates pending
        _processorStarted = false;

        await router.Route(AgentJoinedEvent("call-1"), CancellationToken.None);

        router.ActiveCalls.Should().ContainKey("call-1");
        router.PendingCalls.Should().NotContainKey("call-1");
        _processorStarted.Should().BeTrue();
        // channel contains the buffered leave event then the promoted join event
        CallActor actor = router.ActiveCalls["call-1"];
        actor.EventChannel.Reader.TryRead(out _).Should().BeTrue();
        actor.EventChannel.Reader.TryRead(out _).Should().BeTrue();
    }

    [Fact]
    public async Task Route_BusinessDataToPendingCall_MergesDirectlyNothingWrittenToChannel()
    {
        CallRouter router = CreateRouter();
        await router.Route(BusinessDataEvent("call-1"), CancellationToken.None); // creates pending, no channel events

        await router.Route(BusinessDataEvent("call-1", "skill", "support"), CancellationToken.None);

        CallActor actor = router.PendingCalls["call-1"];
        actor.State.AccumulatedBusinessData.Should().ContainKey("skill").WhoseValue.Should().Be("support");
        actor.EventChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task Route_NonStartNonBusinessToPendingCall_BuffersEventInChannel()
    {
        CallRouter router = CreateRouter();
        await router.Route(BusinessDataEvent("call-1"), CancellationToken.None); // creates pending, no channel events

        await router.Route(AgentLeftEvent("call-1"), CancellationToken.None);

        CallActor actor = router.PendingCalls["call-1"];
        actor.EventChannel.Reader.TryRead(out StreamEvent? ev).Should().BeTrue();
        ev.Should().BeOfType<AgentStreamEvent>();
        router.PendingCalls.Should().ContainKey("call-1");
    }

    [Fact]
    public async Task Route_NewCallIdStartEvent_CreatesActiveCallAndStartsProcessor()
    {
        CallRouter router = CreateRouter();

        await router.Route(AgentJoinedEvent("call-1"), CancellationToken.None);

        router.ActiveCalls.Should().ContainKey("call-1");
        _processorStarted.Should().BeTrue();
        router.ActiveCalls["call-1"].EventChannel.Reader.TryRead(out StreamEvent? ev).Should().BeTrue();
        ev.Should().BeOfType<AgentStreamEvent>();
        router.PendingCalls.Should().NotContainKey("call-1");
    }

    [Fact]
    public async Task Route_NewCallIdBusinessData_CreatesPendingAndMergesDirectly()
    {
        CallRouter router = CreateRouter();

        await router.Route(BusinessDataEvent("call-1"), CancellationToken.None);

        router.PendingCalls.Should().ContainKey("call-1");
        router.PendingCalls["call-1"].State.AccumulatedBusinessData.Should().ContainKey("key");
        router.PendingCalls["call-1"].EventChannel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task Route_NewCallIdNonStartNonBusiness_CreatesPendingAndBuffers()
    {
        CallRouter router = CreateRouter();

        await router.Route(AgentLeftEvent("call-1"), CancellationToken.None);

        router.PendingCalls.Should().ContainKey("call-1");
        router.PendingCalls["call-1"].EventChannel.Reader.TryRead(out _).Should().BeTrue();
        router.ActiveCalls.Should().NotContainKey("call-1");
    }

    [Fact]
    public async Task Route_StalePendingActor_IsRemovedOnNextRouteCall()
    {
        CallRouter router = CreateRouter();
        await router.Route(AgentLeftEvent("stale-call"), CancellationToken.None); // creates pending
        router.PendingCalls["stale-call"].CreatedAt = DateTime.UtcNow.AddHours(-2); // backdate

        await router.Route(AgentLeftEvent("other-call"), CancellationToken.None);

        router.PendingCalls.Should().NotContainKey("stale-call");
    }

    [Fact]
    public async Task Route_FreshPendingActor_NotRemovedOnNextRouteCall()
    {
        CallRouter router = CreateRouter();
        await router.Route(AgentLeftEvent("fresh-call"), CancellationToken.None); // creates pending
        router.PendingCalls["fresh-call"].CreatedAt = DateTime.UtcNow.AddMinutes(-30); // recent

        await router.Route(AgentLeftEvent("other-call"), CancellationToken.None);

        router.PendingCalls.Should().ContainKey("fresh-call");
    }

    [Fact]
    public async Task Route_BusinessDataForActiveCall_WritesEventToChannel()
    {
        CallRouter router = CreateRouter();
        await router.Route(AgentJoinedEvent("call-1"), CancellationToken.None); // creates active
        router.ActiveCalls["call-1"].EventChannel.Reader.TryRead(out _); // drain join event

        await router.Route(BusinessDataEvent("call-1"), CancellationToken.None);

        router.ActiveCalls["call-1"].EventChannel.Reader.TryRead(out StreamEvent? ev).Should().BeTrue();
        ev.Should().BeOfType<BusinessDataStreamEvent>();
    }
}
