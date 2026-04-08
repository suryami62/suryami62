#region

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using suryami62.Components;
using suryami62.Components.Account;
using suryami62.Data;

#endregion

namespace suryami62.Startup;

/// <summary>
///     Applies the web application's middleware pipeline, endpoint registration, and startup tasks.
/// </summary>
internal static partial class WebApplicationExtensions
{
    // Pre-computed security headers for efficiency - avoid rebuilding on every request
    private const string ContentSecurityPolicyValue =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; " +
        "img-src 'self' data: https:; font-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; connect-src 'self' ws: wss:; form-action 'self'";

    private const string ReferrerPolicyValue = "strict-origin-when-cross-origin";
    private const string XContentTypeOptionsValue = "nosniff";
    private const string XFrameOptionsValue = "DENY";
    private const string PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    /// <summary>
    ///     Applies the middleware pipeline used by the web application.
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    /// <returns>The same web application instance for chaining.</returns>
    public static WebApplication UseWebStartupPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ConfigureExceptionHandling(app);

        app.UseForwardedHeaders();
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        // Add response compression early in the pipeline (before static files)
        app.UseResponseCompression();

        app.UseSecurityHeaders();
        app.UseRateLimiter();

        // Add response caching middleware (for static files and API responses)
        app.UseResponseCaching();

        // Add output caching middleware (for Blazor pages - better than response caching for UI)
        app.UseOutputCache();

        app.UseStaticUploads();
        app.UseAntiforgery();

        return app;
    }

    /// <summary>
    ///     Maps the application's static assets, interactive components, and auxiliary endpoints.
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    /// <returns>The same web application instance for chaining.</returns>
    public static WebApplication MapWebEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapAdditionalIdentityEndpoints();
        app.MapSeoEndpoints();

        return app;
    }

    /// <summary>
    ///     Applies pending database migrations before the application starts serving requests.
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    /// <returns>The same web application instance for chaining.</returns>
    public static WebApplication ApplyDatabaseMigrations(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();

            Log.DatabaseMigrationApplied(logger);
        }
        catch (Exception ex)
        {
            Log.DatabaseMigrationFailed(logger, ex);
            throw;
        }

        return app;
    }

    private static void ConfigureExceptionHandling(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            return;
        }

        app.UseExceptionHandler("/Error", true);
        app.UseHsts();
    }

    private static void UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = ContentSecurityPolicyValue;
                headers["Referrer-Policy"] = ReferrerPolicyValue;
                headers["X-Content-Type-Options"] = XContentTypeOptionsValue;
                headers["X-Frame-Options"] = XFrameOptionsValue;
                headers["Permissions-Policy"] = PermissionsPolicyValue;
                return Task.CompletedTask;
            });

            await next().ConfigureAwait(false);
        });
    }

    private static void UseStaticUploads(this WebApplication app)
    {
        var uploadsPath = Path.Combine(app.Environment.WebRootPath, "img", "uploads");
        Directory.CreateDirectory(uploadsPath);

        // Static files with aggressive caching (images rarely change)
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadsPath),
            RequestPath = "/img/uploads",
            OnPrepareResponse = ctx =>
            {
                // Cache images for 7 days - they rarely change
                const int maxAgeSeconds = 604800; // 7 days
                ctx.Context.Response.Headers.CacheControl =
                    $"public,max-age={maxAgeSeconds},immutable";
            }
        });
    }

    /// <summary>
    ///     High-performance logger messages for startup operations.
    /// </summary>
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Applied database migrations at startup.")]
        public static partial void DatabaseMigrationApplied(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "An error occurred while applying database migrations at startup.")]
        public static partial void DatabaseMigrationFailed(ILogger logger, Exception ex);
    }
}