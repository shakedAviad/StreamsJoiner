using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Core.State;
using Xunit;

namespace StreamsJoiner.Tests.Events;

public sealed class CustomerStreamEventTests
{
    private static CustomerStreamEvent JoinEvent(string customerId, string phoneNumber = "1234567890") =>
        new()
        {
            CallId = "call-1",
            Timestamp = "ts",
            CustomerId = customerId,
            PhoneNumber = phoneNumber,
            EventType = CustomerEventType.CUSTOMER_JOINED
        };

    private static CustomerStreamEvent LeaveEvent(string customerId) =>
        new()
        {
            CallId = "call-1",
            Timestamp = "ts",
            CustomerId = customerId,
            PhoneNumber = "1234567890",
            EventType = CustomerEventType.CUSTOMER_LEFT
        };

    [Fact]
    public void ExecuteEvent_FirstCustomerJoins_ReturnsStartedAndSetsState()
    {
        CallState state = new();

        CallStatus? result = JoinEvent("customer-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.STARTED);
        state.ActiveCustomers.Should().Contain("customer-1");
        state.LastCustomerId.Should().Be("customer-1");
        state.LastCustomerPhoneNumber.Should().Be("1234567890");
    }

    [Fact]
    public void ExecuteEvent_CustomerJoinsWhenAgentPresent_ReturnsConnected()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.CurrentStatus = CallStatus.STARTED;

        CallStatus? result = JoinEvent("customer-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.CONNECTED);
    }

    [Fact]
    public void ExecuteEvent_CustomerLeaves_RemovedFromActiveCustomers()
    {
        CallState state = new();
        state.ActiveCustomers.Add("customer-1");
        state.CurrentStatus = CallStatus.STARTED;

        LeaveEvent("customer-1").ExecuteEvent(state, NullLogger.Instance);

        state.ActiveCustomers.Should().NotContain("customer-1");
    }

    [Fact]
    public void ExecuteEvent_CustomerLeaves_LastCustomerPersists()
    {
        CallState state = new();
        state.ActiveCustomers.Add("customer-1");
        state.LastCustomerId = "customer-1";
        state.LastCustomerPhoneNumber = "1234567890";
        state.CurrentStatus = CallStatus.STARTED;

        LeaveEvent("customer-1").ExecuteEvent(state, NullLogger.Instance);

        state.LastCustomerId.Should().Be("customer-1");
        state.LastCustomerPhoneNumber.Should().Be("1234567890");
    }

    [Fact]
    public void ExecuteEvent_CustomerLeavesConnectedCall_ReturnsDisconnected()
    {
        CallState state = new();
        state.ActiveAgents.Add("agent-1");
        state.ActiveCustomers.Add("customer-1");
        state.CurrentStatus = CallStatus.CONNECTED;

        CallStatus? result = LeaveEvent("customer-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().Be(CallStatus.DISCONNECTED);
    }

    [Fact]
    public void ExecuteEvent_DoubleJoin_ReturnsNullAndCountUnchanged()
    {
        CallState state = new();
        state.ActiveCustomers.Add("customer-1");
        state.CurrentStatus = CallStatus.STARTED;

        CallStatus? result = JoinEvent("customer-1").ExecuteEvent(state, NullLogger.Instance);

        result.Should().BeNull();
        state.ActiveCustomers.Should().HaveCount(1);
    }

    [Fact]
    public void ExecuteEvent_LeaveOfUnknownCustomer_ReturnsNull()
    {
        CallState state = new();

        CallStatus? result = LeaveEvent("customer-unknown").ExecuteEvent(state, NullLogger.Instance);

        result.Should().BeNull();
    }
}
