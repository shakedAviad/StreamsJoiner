using FluentAssertions;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Validation;
using Xunit;

namespace StreamsJoiner.Tests.Messaging;

public sealed class StreamEventValidatorTests
{
    private static StreamEventValidator CreateValidator() => new();

    // ValidateAgentEvent

    [Fact]
    public void ValidateAgentEvent_ValidCallId_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "call-123" });

        validator.ValidateAgentEvent(message).Should().BeTrue();
    }

    [Fact]
    public void ValidateAgentEvent_EmptyCallId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "" });

        validator.ValidateAgentEvent(message).Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentEvent_WhitespaceCallId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "   " });

        validator.ValidateAgentEvent(message).Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentEvent_MissingCallIdKey_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>());

        validator.ValidateAgentEvent(message).Should().BeFalse();
    }

    [Fact]
    public void ValidateAgentEvent_OnlyCallIdPresent_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "call-1" });

        validator.ValidateAgentEvent(message).Should().BeTrue();
    }

    // ValidateCustomerEvent

    [Fact]
    public void ValidateCustomerEvent_ValidCallId_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "call-123" });

        validator.ValidateCustomerEvent(message).Should().BeTrue();
    }

    [Fact]
    public void ValidateCustomerEvent_EmptyCallId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "" });

        validator.ValidateCustomerEvent(message).Should().BeFalse();
    }

    [Fact]
    public void ValidateCustomerEvent_MissingCallIdKey_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>());

        validator.ValidateCustomerEvent(message).Should().BeFalse();
    }

    [Fact]
    public void ValidateCustomerEvent_OnlyCallIdPresent_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "call-1" });

        validator.ValidateCustomerEvent(message).Should().BeTrue();
    }

    // ValidateBusinessDataEvent

    [Fact]
    public void ValidateBusinessDataEvent_ValidCallId_ReturnsTrue()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "call-123" });

        validator.ValidateBusinessDataEvent(message).Should().BeTrue();
    }

    [Fact]
    public void ValidateBusinessDataEvent_EmptyCallId_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string> { ["callId"] = "" });

        validator.ValidateBusinessDataEvent(message).Should().BeFalse();
    }

    [Fact]
    public void ValidateBusinessDataEvent_MissingCallIdKey_ReturnsFalse()
    {
        StreamEventValidator validator = CreateValidator();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>());

        validator.ValidateBusinessDataEvent(message).Should().BeFalse();
    }
}
