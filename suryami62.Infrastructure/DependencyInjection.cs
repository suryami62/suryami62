#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using suryami62.Application.Persistence;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure;

/// <summary>
///     Contains dependency injection registration for the infrastructure layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers infrastructure services with performance optimizations.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register base repositories
        services.AddScoped<BlogPostRepository>();
        services.AddScoped<ProjectRepository>();
        services.AddScoped<JourneyHistoryRepository>();
        services.AddScoped<SettingsRepository>();

        // Use optimized/compiled query implementations for read-heavy repositories
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IJourneyHistoryRepository, JourneyHistoryRepository>();

        // Wrap SettingsRepository with memory cache for rarely-changing data
        services.AddScoped<ISettingsRepository>(sp =>
        {
            var inner = sp.GetRequiredService<SettingsRepository>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var logger = sp.GetRequiredService<ILogger<CachedSettingsRepository>>();
            return new CachedSettingsRepository(inner, cache, logger);
        });

        return services;
    }
}