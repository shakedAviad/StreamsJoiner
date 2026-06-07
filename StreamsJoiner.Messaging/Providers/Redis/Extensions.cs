using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StreamsJoiner.Messaging.Interfaces;
using StreamsJoiner.Messaging.Providers.Redis.Parsing;
using StreamsJoiner.Messaging.Providers.Redis.Publishing;
using StreamsJoiner.Messaging.Providers.Redis.Reading;
using StreamsJoiner.Messaging.Providers.Redis.Validation;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("StreamsJoiner.Tests")]

namespace StreamsJoiner.Messaging.Providers.Redis;

public static class Extensions
{
    public static IServiceCollection AddRedisProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            string host = configuration["Redis:Host"] ?? "localhost";
            string port = configuration["Redis:Port"] ?? "6379";
            return ConnectionMultiplexer.Connect($"{host}:{port}");
        });

        services.AddSingleton<StreamEventValidator>();
        services.AddSingleton<AgentEventParser>();
        services.AddSingleton<CustomerEventParser>();
        services.AddSingleton<BusinessDataEventParser>();

        services.AddSingleton<IConsumer>(sp => new RedisConsumer(
            sp.GetRequiredService<IConnectionMultiplexer>(),
            sp.GetRequiredService<AgentEventParser>(),
            sp.GetRequiredService<CustomerEventParser>(),
            sp.GetRequiredService<BusinessDataEventParser>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisConsumer>>()));

        services.AddSingleton<IProducer, RedisProducer>();

        return services;
    }
}
