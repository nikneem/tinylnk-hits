using Microsoft.Extensions.DependencyInjection;
using TinyLink.Hits.Abstractions.Repositories;
using TinyLink.Hits.ExtensionMethods;

namespace TinyLink.Hits.TableStorage.ExtensionMethods;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddTinyLinkHitsWithTableStorage(this IServiceCollection services)
    {
        services.AddTinyLinkHits();
        services.AddScoped<IRawHitsRepository, RawHitsRepository>();
        services.AddScoped<ICalculateHitsRepository, CalculateHitsRepository>();
        services.AddScoped<IHitsTotalRepository, HitsTotalRepository>();
        services.AddScoped<IHitsByTenMinutesRepository, HitsByTenMinutesRepository>();
        return services;
    }

}