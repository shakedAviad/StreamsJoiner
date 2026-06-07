using FluentAssertions;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Validation;
using Xunit;

namespace StreamsJoiner.Tests.Messaging;

public sealed class StreamEventValidatorTests
{
    private static StreamEventValidator CreateValidator() => new();

    private static Dictionary<string, string> ValidAgentFields() =>
        new()
        {
            ["callId"] = "call-1",
            ["agentId"] = "agent-1",
            ["agentName"] = "Alice",
            ["eventType"] = "AGENT_JOINED"
        };

    private static Dictionary<string, string> ValidCustomerFields() =>
        new()
        {
            ["callId"] = "call-1",
            ["customerId"] = "cust-1",
            ["phoneNumber"] = "555-0001",
            ["eventType"] = "CUSTOMER_JOINED"
        };

    [Fact]
    public void ValidateAgentEvent_AllRequiredFieldsPresent_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", ValidAgentFields());

        bool result = validator.ValidateAgentEvent(message);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateAgentEvent_EmptyCallId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        Dictionary<string, string> fields = ValidAgentFields();
        fields["callId"] = "";
        RawStreamMessage message = new("msg-1", fields);

        bool result = validator.ValidateAgentEvent(message);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentEvent_MissingCallId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        Dictionary<string, string> fields = ValidAgentFields();
        fields.Remove("callId");
        RawStreamMessage message = new("msg-1", fields);

        bool result = validator.ValidateAgentEvent(message);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentEvent_MissingAgentId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        Dictionary<string, string> fields = ValidAgentFields();
        fields.Remove("agentId");
        RawStreamMessage message = new("msg-1", fields);

        bool result = validator.ValidateAgentEvent(message);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentEvent_InvalidEventType_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        Dictionary<string, string> fields = ValidAgentFields();
        fields["eventType"] = "UNKNOWN";
        RawStreamMessage message = new("msg-1", fields);

        bool result = validator.ValidateAgentEvent(message);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCustomerEvent_AllRequiredFieldsPresent_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", ValidCustomerFields());

        bool result = validator.ValidateCustomerEvent(message);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCustomerEvent_MissingPhoneNumber_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        Dictionary<string, string> fields = ValidCustomerFields();
        fields.Remove("phoneNumber");
        RawStreamMessage message = new("msg-1", fields);

        bool result = validator.ValidateCustomerEvent(message);

        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBusinessDataEvent_OnlyCallIdPresent_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "call-1" });

        bool result = validator.ValidateBusinessDataEvent(message);

        result.Should().BeTrue();
    }
}
