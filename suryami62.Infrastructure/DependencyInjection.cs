#region

using Microsoft.Extensions.DependencyInjection;
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
    ///     Registers infrastructure services.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        return services;
    }
}