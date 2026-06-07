using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StreamsJoiner.Core.Events;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Parsing;
using StreamsJoiner.Messaging.Providers.Redis.Validation;
using Xunit;

namespace StreamsJoiner.Tests.Messaging;

public sealed class BusinessDataEventParserTests
{
    private static BusinessDataEventParser CreateParser() =>
        new(new StreamEventValidator(), NullLogger<BusinessDataEventParser>.Instance);

    [Fact]
    public void Parse_MessageWithExtraFields_DataContainsOnlyBusinessKeys()
    {
        BusinessDataEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>
        {
            ["callId"] = "call-1",
            ["timestamp"] = "2026-01-01T00:00:00Z",
            ["skillName"] = "routing",
            ["priority"] = "high"
        });

        BusinessDataStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.Data.Should().ContainKey("skillName").WhoseValue.Should().Be("routing");
        result.Data.Should().ContainKey("priority").WhoseValue.Should().Be("high");
        result.Data.Should().NotContainKey("callId");
        result.Data.Should().NotContainKey("timestamp");
    }

    [Fact]
    public void Parse_ValidMessage_SetsCallIdAndTimestampCorrectly()
    {
        BusinessDataEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>
        {
            ["callId"] = "call-42",
            ["timestamp"] = "2026-06-07T10:00:00Z",
            ["key1"] = "value1"
        });

        BusinessDataStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.CallId.Should().Be("call-42");
        result.Timestamp.Should().Be("2026-06-07T10:00:00Z");
    }

    [Fact]
    public void Parse_MessageWithOnlyCallIdAndTimestamp_ReturnsEventWithEmptyData()
    {
        BusinessDataEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>
        {
            ["callId"] = "call-1",
            ["timestamp"] = "2026-01-01T00:00:00Z"
        });

        BusinessDataStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.Data.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MessageWithFiveBusinessKeys_AllAppearInData()
    {
        BusinessDataEventParser parser = CreateParser();
        RawStreamMessage message = new("msg-1", new Dictionary<string, string>
        {
            ["callId"] = "call-1",
            ["key1"] = "v1",
            ["key2"] = "v2",
            ["key3"] = "v3",
            ["key4"] = "v4",
            ["key5"] = "v5"
        });

        BusinessDataStreamEvent? result = parser.Parse(message);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(5);
        result.Data.Should().ContainKey("key1");
        result.Data.Should().ContainKey("key5");
    }
}
