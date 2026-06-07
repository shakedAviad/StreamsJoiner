using StreamsJoiner.Core.Models;

namespace StreamsJoiner.Services;

internal sealed class CallJoinerService : BackgroundService
{
    private readonly ILogger<CallJoinerService> _logger;

    public CallJoinerService(ILogger<CallJoinerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CallJoinerService started. Implement your stream-joining logic here.");

        // ---------------------------------------------------------------------
        // Your implementation goes here.
        //
        // Suggested starting points:
        //   - Read from the input streams: "agent-events", "customer-events",
        //     "business-data-events"
        //   - Parse entries into AgentEvent / CustomerEvent / BusinessDataEvent.
        //   - Correlate by callId and track each call's state.
        //   - Publish CallEvent.ToMap() to the "joined-call-events" output stream.
        // ---------------------------------------------------------------------

        // Keep the service alive until shutdown is requested.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
