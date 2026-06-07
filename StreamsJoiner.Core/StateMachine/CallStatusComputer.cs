using StreamsJoiner.Core.Models;

namespace StreamsJoiner.Core.StateMachine;

public sealed class CallStatusComputer
{
    public CallStatus? Compute(int agentCount, int customerCount, CallStatus? previous)
    {
        if (agentCount == 0 && customerCount == 0)
        {
            return previous is null ? null : CallStatus.ENDED;
        }

        if (agentCount > 0 && customerCount > 0)
        {
            return previous == CallStatus.CONNECTED ? null : CallStatus.CONNECTED;
        }

        // One side only (agentCount > 0 XOR customerCount > 0)
        if (previous == CallStatus.CONNECTED)
        {
            return CallStatus.DISCONNECTED;
        }

        if (previous is null)
        {
            return CallStatus.STARTED;
        }

        return null;
    }
}
