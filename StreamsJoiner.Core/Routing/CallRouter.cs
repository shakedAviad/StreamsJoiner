using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;

namespace StreamsJoiner.Core.Routing;

public sealed class CallRouter(
    ILogger<CallRouter> logger,
    Func<string, CallActor, CancellationToken, Task> processorFactory)
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, CallActor> _activeCalls = new();
    private readonly ConcurrentDictionary<string, CallActor> _pendingCalls = new();

    public IReadOnlyDictionary<string, CallActor> ActiveCalls => _activeCalls;
    public IReadOnlyDictionary<string, CallActor> PendingCalls => _pendingCalls;

    // Called by the processor when its inactivity timeout expires.
    public void RemoveFromActive(string callId) => _activeCalls.TryRemove(callId, out _);

    public async Task Route(StreamEvent streamEvent, CancellationToken ct)
    {
        CleanupStalePending();

        string callId = streamEvent.CallId;

        if (_activeCalls.TryGetValue(callId, out CallActor? activeActor))
        {
            await activeActor.EventChannel.Writer.WriteAsync(streamEvent, ct);
            return;
        }

        if (_pendingCalls.TryGetValue(callId, out CallActor? pendingActor))
        {
            await HandlePendingCall(pendingActor, callId, streamEvent, ct);
            return;
        }

        await HandleNewCall(callId, streamEvent, ct);
    }

    private async Task HandlePendingCall(
        CallActor pendingActor,
        string callId,
        StreamEvent streamEvent,
        CancellationToken ct)
    {
        if (IsStartEvent(streamEvent))
        {
            if (_pendingCalls.TryRemove(callId, out CallActor? promoted))
            {
                _activeCalls.TryAdd(callId, promoted);
                await promoted.EventChannel.Writer.WriteAsync(streamEvent, ct);
                promoted.ProcessorTask = processorFactory(callId, promoted, ct);
            }
        }
        else if (streamEvent is BusinessDataStreamEvent businessData)
        {
            foreach (KeyValuePair<string, string> entry in businessData.Data)
            {
                pendingActor.State.AccumulatedBusinessData[entry.Key] = entry.Value;
            }
        }
        else
        {
            await pendingActor.EventChannel.Writer.WriteAsync(streamEvent, ct);
        }
    }

    private async Task HandleNewCall(string callId, StreamEvent streamEvent, CancellationToken ct)
    {
        CallActor newActor = new();

        if (IsStartEvent(streamEvent))
        {
            CallActor actor = _activeCalls.GetOrAdd(callId, newActor);
            await actor.EventChannel.Writer.WriteAsync(streamEvent, ct);
            if (actor.ProcessorTask is null)
            {
                actor.ProcessorTask = processorFactory(callId, actor, ct);
            }
        }
        else if (streamEvent is BusinessDataStreamEvent businessData)
        {
            CallActor actor = _pendingCalls.GetOrAdd(callId, newActor);
            foreach (KeyValuePair<string, string> entry in businessData.Data)
            {
                actor.State.AccumulatedBusinessData[entry.Key] = entry.Value;
            }
        }
        else
        {
            CallActor actor = _pendingCalls.GetOrAdd(callId, newActor);
            await actor.EventChannel.Writer.WriteAsync(streamEvent, ct);
        }
    }

    private void CleanupStalePending()
    {
        DateTime cutoff = DateTime.UtcNow - StaleThreshold;
        foreach (KeyValuePair<string, CallActor> entry in _pendingCalls)
        {
            if (entry.Value.CreatedAt < cutoff && _pendingCalls.TryRemove(entry.Key, out _))
            {
                logger.LogInformation("Removed stale pending call {CallId}", entry.Key);
            }
        }
    }

    private static bool IsStartEvent(StreamEvent streamEvent) =>
        streamEvent is AgentStreamEvent { EventType: AgentEventType.AGENT_JOINED } ||
        streamEvent is CustomerStreamEvent { EventType: CustomerEventType.CUSTOMER_JOINED };
}
