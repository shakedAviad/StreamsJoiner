using StreamsJoiner.Core.Processing;
using StreamsJoiner.Core.Routing;
using StreamsJoiner.Core.State;
using StreamsJoiner.Core.StateMachine;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis;
using StreamsJoiner.Services;

namespace StreamsJoiner;

internal sealed class ProcessorFactoryHolder
{
    internal Func<string, CallActor, CancellationToken, Task> Factory { get; set; } =
        (_, _, _) => Task.CompletedTask;
}

internal static class Extensions
{
    internal static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder builder)
    {
        builder.Services.AddRedisProviders(builder.Configuration);

        builder.Services.AddSingleton<CallStatusComputer>();

        builder.Services.AddSingleton<ProcessorFactoryHolder>();

        builder.Services.AddSingleton<EventProcessor>(sp => new EventProcessor(
            sp.GetRequiredService<ILogger<EventProcessor>>(),
            sp.GetRequiredService<CallRouter>().RemoveFromActive,
            sp.GetRequiredService<IProducer>().ProduceAsync,
            TimeSpan.FromHours(1)));

        builder.Services.AddSingleton<CallRouter>(sp =>
        {
            ProcessorFactoryHolder holder = sp.GetRequiredService<ProcessorFactoryHolder>();
            return new CallRouter(
                sp.GetRequiredService<ILogger<CallRouter>>(),
                (callId, actor, ct) => holder.Factory(callId, actor, ct));
        });

        builder.Services.AddHostedService<CallJoinerService>();

        return builder;
    }
}
