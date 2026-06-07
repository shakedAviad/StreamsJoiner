using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Parsing;
using StreamsJoiner.Messaging.Providers.Redis.Validation;
using Xunit;

namespace StreamsJoiner.Tests.Messaging;

public sealed class AgentEventParserTests
{
    private static AgentEventParser CreateParser() =>
        new(new StreamEventValidator(), NullLogger<AgentEventParser>.Instance);

    private static Dictionary<string, string> ValidFields(string eventType = "AGENT_JOINED") =>
        new()
        {
            ["callId"] = "call-1",
            ["agentId"] = "agent-1",
            ["agentName"] = "Alice",
            ["eventType"] = eventType,
            ["timestamp"] = "2026-01-01T00:00:00Z"
        };

    [Fact]
    public void Parse_ValidMessage_ReturnsAgentStreamEventWithAllFields()
    {
        AgentEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", ValidFields());

        AgentStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.CallId.Should().Be("call-1");
        result.AgentId.Should().Be("agent-1");
        result.AgentName.Should().Be("Alice");
        result.EventType.Should().Be(AgentEventType.AGENT_JOINED);
        result.Timestamp.Should().Be("2026-01-01T00:00:00Z");
    }

    [Fact]
    public void Parse_MissingCallId_ReturnsNull()
    {
        AgentEventParser parser = CreateParser();
        Dictionary<string, string> fields = ValidFields();
        fields.Remove("callId");
        RawStreamMessage message = new("msg-1", fields);

        AgentStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyCallId_ReturnsNull()
    {
        AgentEventParser parser = CreateParser();
        Dictionary<string, string> fields = ValidFields();
        fields["callId"] = "";
        RawStreamMessage message = new("msg-1", fields);

        AgentStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_MissingAgentId_ReturnsNull()
    {
        AgentEventParser parser = CreateParser();
        Dictionary<string, string> fields = ValidFields();
        fields.Remove("agentId");
        RawStreamMessage message = new("msg-1", fields);

        AgentStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidEventType_ReturnsNull()
    {
        AgentEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", ValidFields("INVALID_VALUE"));

        AgentStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_AgentLeftEventType_ReturnsEventWithAgentLeft()
    {
        AgentEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", ValidFields("AGENT_LEFT"));

        AgentStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.EventType.Should().Be(AgentEventType.AGENT_LEFT);
    }
}
