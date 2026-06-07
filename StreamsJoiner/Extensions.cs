using StreamsJoiner.Services;

namespace StreamsJoiner;

internal static class Extensions
{
    internal static HostApplicationBuilder ConfigureServices(this HostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<CallJoinerService>();
        return builder;
    }
}
