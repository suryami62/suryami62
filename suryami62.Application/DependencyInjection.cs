#region

using Microsoft.Extensions.DependencyInjection;
using suryami62.Services;

#endregion

namespace suryami62.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBlogPostService, BlogPostService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<UserInfoSettingsStore>();
        services.AddScoped<ApplicationSettingsStore>();

        return services;
    }
}