#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using suryami62.Application.Persistence;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<BlogPostRepository>();
        services.AddScoped<ProjectRepository>();
        services.AddScoped<JourneyHistoryRepository>();
        services.AddScoped<SettingsRepository>();

        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IJourneyHistoryRepository, JourneyHistoryRepository>();

        services.AddScoped<ISettingsRepository>(serviceProvider =>
        {
            var innerRepository = serviceProvider.GetRequiredService<SettingsRepository>();

            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            var logger = serviceProvider.GetRequiredService<ILogger<CachedSettingsRepository>>();

            var decoratedRepository = new CachedSettingsRepository(
                innerRepository,
                memoryCache,
                logger);

            return decoratedRepository;
        });

        return services;
    }
}