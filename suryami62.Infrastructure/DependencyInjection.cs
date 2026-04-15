// ============================================================================
// INFRASTRUCTURE DEPENDENCY INJECTION
// ============================================================================
// This file configures dependency injection (DI) for the Infrastructure layer.
// DI = Dependency Injection = "don't create objects, ask for them"
//
// SERVICE LIFETIMES:
// - Singleton: One instance for entire app lifetime
// - Scoped: One instance per HTTP request
// - Transient: New instance every time requested
//
// REPOSITORY PATTERN REGISTRATION:
// We register each repository TWICE:
// 1. Concrete class (e.g., BlogPostRepository) - for internal use/decorators
// 2. Interface → Implementation (e.g., IBlogPostRepository → BlogPostRepository)
//    - for external code (Controllers, Services)
//
// DECORATOR PATTERN - Settings Repository:
// ISettingsRepository is wrapped in CachedSettingsRepository (decorator).
// Decorator adds caching behavior on TOP of the base repository.
// Request flow: Caller → CachedSettingsRepository → SettingsRepository → Database
//
// WHY SCOPE = SCOPED?
// DbContext is scoped (one per request). Repositories use DbContext, so they
// must also be scoped. Same lifetime = can share DbContext instance.
// ============================================================================

#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using suryami62.Application.Persistence;
using suryami62.Infrastructure.Persistence;

#endregion

namespace suryami62.Infrastructure;

/// <summary>
///     Configures dependency injection for Infrastructure layer services.
///     Extension method on IServiceCollection for fluent registration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all Infrastructure layer services with the DI container.
    ///     Call this from Program.cs: services.AddInfrastructureServices();
    /// </summary>
    /// <param name="services">The DI container to add services to.</param>
    /// <returns>The same IServiceCollection for method chaining (fluent API).</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Step 1: Validate input
        ArgumentNullException.ThrowIfNull(services);

        // Step 2: Register concrete repository implementations
        // These are the actual classes that do the database work
        // Register as Scoped = one instance per HTTP request
        services.AddScoped<BlogPostRepository>();
        services.AddScoped<ProjectRepository>();
        services.AddScoped<JourneyHistoryRepository>();
        services.AddScoped<SettingsRepository>();

        // Step 3: Register interfaces pointing to implementations
        // External code depends on interfaces (IBlogPostRepository), not concrete classes
        // This allows swapping implementations without changing calling code
        services.AddScoped<IBlogPostRepository, BlogPostRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IJourneyHistoryRepository, JourneyHistoryRepository>();

        // Step 4: Register ISettingsRepository with caching decorator
        // Manual factory lambda (sp => ...) to control object construction
        // Decorator pattern: CachedSettingsRepository wraps SettingsRepository
        services.AddScoped<ISettingsRepository>(serviceProvider =>
        {
            // Step 4a: Get the inner/base repository from DI container
            // GetRequiredService<T>() throws if T not registered (fails fast)
            var innerRepository = serviceProvider.GetRequiredService<SettingsRepository>();

            // Step 4b: Get supporting services for the decorator
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            var logger = serviceProvider.GetRequiredService<ILogger<CachedSettingsRepository>>();

            // Step 4c: Create and return the decorated repository
            // Caller gets CachedSettingsRepository, which wraps inner SettingsRepository
            var decoratedRepository = new CachedSettingsRepository(
                innerRepository,
                memoryCache,
                logger);

            return decoratedRepository;
        });

        // Step 5: Return IServiceCollection for fluent chaining
        // Example: services.AddInfrastructureServices().AddOtherServices();
        return services;
    }
}