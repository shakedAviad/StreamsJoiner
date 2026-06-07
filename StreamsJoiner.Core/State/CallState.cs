using System.Collections.Concurrent;
using StreamsJoiner.Core.Models;

namespace StreamsJoiner.Core.State;

public sealed class CallState
{
    public HashSet<string> ActiveAgents { get; } = [];
    public HashSet<string> ActiveCustomers { get; } = [];
    public string? LastAgentId { get; set; }
    public string? LastAgentName { get; set; }
    public string? LastCustomerId { get; set; }
    public string? LastCustomerPhoneNumber { get; set; }
    public CallStatus? CurrentStatus { get; set; }
    public ConcurrentDictionary<string, string> AccumulatedBusinessData { get; } = new();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
