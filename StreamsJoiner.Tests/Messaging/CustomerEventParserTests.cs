using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Core.Models;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Parsing;
using StreamsJoiner.Messaging.Providers.Redis.Validation;
using Xunit;

namespace StreamsJoiner.Tests.Messaging;

public sealed class CustomerEventParserTests
{
    private static CustomerEventParser CreateParser() =>
        new(new StreamEventValidator(), NullLogger<CustomerEventParser>.Instance);

    private static Dictionary<string, string> ValidFields(string eventType = "CUSTOMER_JOINED") =>
        new()
        {
            ["callId"] = "call-1",
            ["customerId"] = "cust-1",
            ["phoneNumber"] = "555-0001",
            ["eventType"] = eventType,
            ["timestamp"] = "2026-01-01T00:00:00Z"
        };

    [Fact]
    public void Parse_ValidMessage_ReturnsCustomerStreamEventWithAllFields()
    {
        CustomerEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", ValidFields());

        CustomerStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.CallId.Should().Be("call-1");
        result.CustomerId.Should().Be("cust-1");
        result.PhoneNumber.Should().Be("555-0001");
        result.EventType.Should().Be(CustomerEventType.CUSTOMER_JOINED);
        result.Timestamp.Should().Be("2026-01-01T00:00:00Z");
    }

    [Fact]
    public void Parse_MissingCustomerId_ReturnsNull()
    {
        CustomerEventParser parser = CreateParser();
        Dictionary<string, string> fields = ValidFields();
        fields.Remove("customerId");
        RawStreamMessage message = new("msg-1", fields);

        CustomerStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_MissingPhoneNumber_ReturnsNull()
    {
        CustomerEventParser parser = CreateParser();
        Dictionary<string, string> fields = ValidFields();
        fields.Remove("phoneNumber");
        RawStreamMessage message = new("msg-1", fields);

        CustomerStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidEventType_ReturnsNull()
    {
        CustomerEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", ValidFields("INVALID_VALUE"));

        CustomerStreamEvent? result = parser.Parse(message);

        result.Should().BeNull();
    }
}
