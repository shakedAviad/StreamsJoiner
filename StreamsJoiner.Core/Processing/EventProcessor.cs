using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;

namespace StreamsJoiner.Core.Processing;

public sealed class EventProcessor(
    ILogger<EventProcessor> logger,
    Action<string> removeFromActive,
    Func<CallEvent, CancellationToken, Task> publishAsync,
    TimeSpan? inactivityTimeout = null)
{
    private readonly TimeSpan _inactivityTimeout = inactivityTimeout ?? TimeSpan.FromHours(1);

    public async Task ProcessAsync(string callId, CallActor actor, CancellationToken ct)
    {
        try
        {
            while (true)
            {
                bool hasItem;
                try
                {
                    using CancellationTokenSource timeoutCts = new(_inactivityTimeout);
                    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                    hasItem = await actor.EventChannel.Reader.WaitToReadAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    logger.LogInformation("Call {CallId} processor exiting: inactivity timeout", callId);
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (!hasItem)
                {
                    return;
                }

                while (actor.EventChannel.Reader.TryRead(out StreamEvent? streamEvent))
                {
                    CallStatus? newStatus = streamEvent.ExecuteEvent(actor.State, logger);
                    if (newStatus is { } status)
                    {
                        CallEvent callEvent = BuildSnapshot(callId, status, actor.State, streamEvent.Timestamp);
                        await publishAsync(callEvent, ct);
                        if (status == CallStatus.ENDED)
                        {
                            logger.LogInformation("Call {CallId} processor exiting: call ended", callId);
                            return;
                        }
                    }
                }
            }
        }
        finally
        {
            removeFromActive(callId);
        }
    }

    private static CallEvent BuildSnapshot(string callId, CallStatus status, CallState state, string timestamp) =>
        new()
        {
            CallId = callId,
            Status = status,
            AgentId = state.LastAgentId,
            AgentName = state.LastAgentName,
            CustomerId = state.LastCustomerId,
            PhoneNumber = state.LastCustomerPhoneNumber,
            BusinessData = new Dictionary<string, string>(state.AccumulatedBusinessData),
            Timestamp = timestamp
        };
}
