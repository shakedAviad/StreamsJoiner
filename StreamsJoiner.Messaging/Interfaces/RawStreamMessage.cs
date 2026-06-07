namespace StreamsJoiner.Messaging.Interfaces;

public sealed record RawStreamMessage(
    string MessageId,
    Dictionary<string, string> Fields);
