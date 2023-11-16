using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TinyLink.Hits.TableStorage.ExtensionMethods;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
        var config = builder.Build();
        Console.WriteLine(config["ServiceBus"]);
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddTinyLinkHitsWithTableStorage();
    })
    .Build();

host.Run();
