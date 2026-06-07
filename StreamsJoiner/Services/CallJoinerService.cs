using StreamsJoiner.Core.Processing;
using StreamsJoiner.Core.Routing;
using StreamsJoiner.Messaging.Interfaces;

namespace StreamsJoiner.Services;

internal sealed class CallJoinerService(
    IConsumer consumer,
    CallRouter router,
    EventProcessor processor,
    ProcessorFactoryHolder holder,
    ILogger<CallJoinerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        holder.Factory = (callId, actor, ct) =>
            Task.Run(() => processor.ProcessAsync(callId, actor, ct), ct);

        logger.LogInformation("{Service} starting", nameof(CallJoinerService));

        Task agentTask = consumer.ConsumeAsync("agent-events", router.Route, stoppingToken);
        Task customerTask = consumer.ConsumeAsync("customer-events", router.Route, stoppingToken);
        Task businessTask = consumer.ConsumeAsync("business-data-events", router.Route, stoppingToken);

        try
        {
            await Task.WhenAll(agentTask, customerTask, businessTask);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in consumer loops");
        }
    }
}
