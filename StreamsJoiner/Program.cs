using StreamsJoiner;

await Host.CreateApplicationBuilder(args)
    .ConfigureServices()
    .Build()
    .RunAsync();
