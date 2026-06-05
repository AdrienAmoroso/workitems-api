using Serilog;
using Serilog.Formatting.Json;
using WorkItems.Worker.Consumers;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, config) =>
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console(new JsonFormatter()))
    .ConfigureServices((_, services) =>
    {
        services.AddHostedService<WorkItemEventProcessor>();
    })
    .Build();

await host.RunAsync();
