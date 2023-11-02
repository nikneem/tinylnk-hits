using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TinyLink.Hits.TableStorage.ExtensionMethods;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddTinyLinkHitsWithTableStorage();
    })
    .Build();

host.Run();
