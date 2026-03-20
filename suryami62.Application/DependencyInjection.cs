#region

using Microsoft.Extensions.DependencyInjection;
using suryami62.Services;

#endregion

namespace suryami62.Application;

public static class ApplicationServiceCollectionExtensions
{
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