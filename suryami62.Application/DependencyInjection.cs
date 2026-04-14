// ============================================================================
// APPLICATION DEPENDENCY INJECTION
// ============================================================================
// This file registers application-layer services with the Dependency Injection (DI) container.
//
// WHAT IS DEPENDENCY INJECTION?
// DI is a design pattern where objects receive their dependencies from outside
// rather than creating them internally. This makes code:
// - Easier to test (can inject fake/mock dependencies)
// - More flexible (can swap implementations)
// - More maintainable (dependencies are explicit)
//
// SERVICE LIFETIMES:
// - AddScoped: One instance per HTTP request
//   Use for: Services that should be reused within a single request
// - AddTransient: New instance every time requested
//   Use for: Lightweight services with no state
// - AddSingleton: One instance for entire application
//   Use for: Shared resources, configuration, caching
//
// INTERFACE vs CONCRETE REGISTRATION:
// services.AddScoped<IBlogPostService, BlogPostService>()
//   ^ When code asks for IBlogPostService, give them BlogPostService
//   This allows swapping BlogPostService with a different implementation
//
// services.AddScoped<ApplicationSettingsStore>()
//   ^ Register concrete class directly
//   Use when there's no interface or only one implementation
// ============================================================================

#region

using Microsoft.Extensions.DependencyInjection;
using suryami62.Services;

#endregion

namespace suryami62.Application;

/// <summary>
///     Extension methods for registering application services with the DI container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all application-layer services with the DI container.
    ///     Call this from Program.cs to set up the application services.
    /// </summary>
    /// <param name="services">The DI container service collection.</param>
    /// <returns>The same service collection (for method chaining).</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register blog post service
        // When any class needs IBlogPostService, provide BlogPostService instance
        // Scoped = same instance reused within a single HTTP request
        services.AddScoped<IBlogPostService, BlogPostService>();

        // Register project/portfolio service
        services.AddScoped<IProjectService, ProjectService>();

        // Register journey history (timeline) service
        services.AddScoped<IJourneyHistoryService, JourneyHistoryService>();

        // Register site profile settings store (concrete class)
        services.AddScoped<SiteProfileSettingsStore>();

        // Register the same SiteProfileSettingsStore when ISiteProfileSettingsStore is requested
        // This allows both the concrete class and interface to resolve to the same instance
        services.AddScoped<ISiteProfileSettingsStore>(serviceProvider =>
            serviceProvider.GetRequiredService<SiteProfileSettingsStore>());

        // Register application settings store (registration enabled/disabled)
        services.AddScoped<ApplicationSettingsStore>();

        return services;
    }
}