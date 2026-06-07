using Microsoft.Extensions.Logging;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;
using StreamsJoiner.Core.StateMachine;

namespace StreamsJoiner.Core.Events;

public sealed class CustomerStreamEvent : StreamEvent
{
    public string CustomerId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public CustomerEventType EventType { get; init; }

    public override CallStatus? ExecuteEvent(CallState state, ILogger logger)
    {
        if (EventType == CustomerEventType.CUSTOMER_JOINED)
        {
            if (state.ActiveCustomers.Contains(CustomerId))
            {
                return null;
            }
            state.ActiveCustomers.Add(CustomerId);
            state.LastCustomerId = CustomerId;
            state.LastCustomerPhoneNumber = PhoneNumber;
        }
        else
        {
            if (!state.ActiveCustomers.Contains(CustomerId))
            {
                logger.LogWarning("Leave event for unknown customer {CustomerId} on call {CallId}", CustomerId, CallId);
                return null;
            }
            state.ActiveCustomers.Remove(CustomerId);
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
