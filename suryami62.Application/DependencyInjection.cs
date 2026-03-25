#region

using Microsoft.Extensions.DependencyInjection;
using suryami62.Services;

#endregion

namespace suryami62.Application;

/// <summary>
///     Provides dependency injection registration for the application layer.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    ///     Registers application services and settings stores used by the site.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBlogPostService, BlogPostService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IJourneyHistoryService, JourneyHistoryService>();
        services.AddScoped<SiteProfileSettingsStore>();
        services.AddScoped<ApplicationSettingsStore>();

        return services;
    }
}