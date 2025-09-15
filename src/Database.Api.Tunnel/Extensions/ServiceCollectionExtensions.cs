using Database.Api.Tunnel.Services;
using Database.Api.Tunnel.Services.Abstractions;

namespace Database.Api.Tunnel.Extensions;

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<ISqlService, SqlService>();
        services.AddScoped<ICosmosService, CosmosService>();

        return services;
    }
}