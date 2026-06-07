namespace StreamsJoiner.Core.Models;

/// <summary>
/// Represents an event from the "customer-events" stream.
/// Customers call in to the call center or hang up.
/// </summary>
public class CustomerEvent
{
    public string? CallId { get; set; }
    public CustomerEventType EventType { get; set; }
    public string? CustomerId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Timestamp { get; set; }

    public CustomerEvent() { }

    public CustomerEvent(string? callId, CustomerEventType eventType, string? customerId, string? phoneNumber, string? timestamp)
    {
        CallId = callId;
        EventType = eventType;
        CustomerId = customerId;
        PhoneNumber = phoneNumber;
        Timestamp = timestamp;
    }

    public override string ToString() =>
        $"CustomerEvent{{callId='{CallId}', eventType='{EventType}', customerId='{CustomerId}', phoneNumber='{PhoneNumber}', timestamp='{Timestamp}'}}";
}
