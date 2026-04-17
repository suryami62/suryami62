// ============================================================================
// WEB APPLICATION EXTENSIONS
// ============================================================================
// This file contains extension methods that configure the application's
// middleware pipeline and endpoints.
//
// WHAT IS MIDDLEWARE?
// Middleware is software that processes HTTP requests and responses.
// Each middleware component can:
// 1. Process the incoming request
// 2. Pass the request to the next middleware
// 3. Process the outgoing response on the way back
//
// THE PIPELINE ORDER MATTERS:
// Middleware runs in the order added. For example:
// - Exception handling should be first (catches errors from everything)
// - Authentication must come before authorization
// - Static files should come early (fast path for images/CSS)
// ============================================================================

#region

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using suryami62.Components;
using suryami62.Components.Account;
using suryami62.Data;

#endregion

namespace suryami62.Startup;

/// <summary>
///     Extension methods for configuring the web application's middleware and endpoints.
/// </summary>
internal static class WebApplicationExtensions
{
    // Security header values - defined as constants for efficiency
    // (avoids rebuilding strings on every request)

    /// <summary>
    ///     Content Security Policy (CSP) restricts what resources can load.
    ///     This prevents XSS attacks by blocking inline scripts and external resources.
    /// </summary>
    private const string ContentSecurityPolicyValue =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none; " +
        "img-src 'self' data: https:; font-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; connect-src 'self' ws: wss:; form-action 'self'";

    /// <summary>
    ///     Controls how much referrer information is sent with requests.
    ///     "strict-origin-when-cross-origin" sends full URL to same origin, only origin to others.
    /// </summary>
    private const string ReferrerPolicyValue = "strict-origin-when-cross-origin";

    /// <summary>
    ///     Prevents browsers from MIME-sniffing responses (security feature).
    /// </summary>
    private const string XContentTypeOptionsValue = "nosniff";

    /// <summary>
    ///     Prevents the page from being embedded in frames (clickjacking protection).
    /// </summary>
    private const string XFrameOptionsValue = "DENY";

    /// <summary>
    ///     Controls access to browser features (camera, microphone, geolocation).
    ///     Empty parentheses mean "disabled".
    /// </summary>
    private const string PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    /// <summary>
    ///     Configures the middleware pipeline - the order matters!
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    public static WebApplication UseWebStartupPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Step 1: Exception handling (first so it catches all errors)
        ConfigureExceptionHandling(app);

        // Step 2: Handle forwarded headers (for proxy/load balancer scenarios)
        app.UseForwardedHeaders();

        // Step 3: Custom status code pages (404, 500, etc.)
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        // Step 4: Redirect HTTP to HTTPS
        app.UseHttpsRedirection();

        // Step 5: Compress responses (before static files for efficiency)
        app.UseResponseCompression();

        // Step 6: Add security headers (CSP, X-Frame-Options, etc.)
        app.UseSecurityHeaders();

        // Step 7: Rate limiting (prevent brute force attacks)
        app.UseRateLimiter();

        // Step 8: Response caching (for API responses)
        app.UseResponseCaching();

        // Step 9: Output caching (for Blazor pages)
        app.UseOutputCache();

        // Step 10: Serve uploaded images
        app.UseStaticUploads();

        // Step 11: Anti-forgery token validation (CSRF protection)
        app.UseAntiforgery();

        return app;
    }

    /// <summary>
    ///     Maps all application endpoints (routes).
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    public static WebApplication MapWebEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Map static assets (CSS, JS, images with fingerprinting)
        app.MapStaticAssets();

        // Map Razor Components (Blazor pages) with interactive server mode
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Map Identity endpoints (login, register, logout)
        app.MapAdditionalIdentityEndpoints();

        // Map SEO endpoints (sitemap.xml, robots.txt)
        app.MapSeoEndpoints();

        return app;
    }

    /// <summary>
    ///     Applies database migrations at startup.
    ///     This ensures the database schema is up to date before serving requests.
    /// </summary>
    /// <param name="app">The web application being configured.</param>
    public static WebApplication ApplyDatabaseMigrations(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Create a service scope to access scoped services (like DbContext)
        using var scope = app.Services.CreateScope();

        // Get logger for this operation
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Get the database context and apply migrations
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();

            Log.DatabaseMigrationApplied(logger);
        }
        catch (Exception ex)
        {
            Log.DatabaseMigrationFailed(logger, ex);
            throw; // Re-throw to prevent app from starting with bad database
        }

        return app;
    }

    /// <summary>
    ///     Configures exception handling based on environment.
    ///     Development: Shows detailed error pages
    ///     Production: Shows generic error page (security)
    /// </summary>
    private static void ConfigureExceptionHandling(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Development: Show detailed migration error pages
            app.UseMigrationsEndPoint();
        }
        else
        {
            // Production: Generic error handler (don't leak details)
            app.UseExceptionHandler("/Error", true);
            app.UseHsts(); // Enforce HTTPS
        }
    }

    /// <summary>
    ///     Adds security headers to all HTTP responses.
    ///     These headers help protect against common web attacks.
    /// </summary>
    private static void UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Register a callback to run when response starts
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // Content Security Policy - restricts what can load
                headers["Content-Security-Policy"] = ContentSecurityPolicyValue;

                // Referrer Policy - controls referrer information
                headers["Referrer-Policy"] = ReferrerPolicyValue;

                // Prevent MIME sniffing
                headers["X-Content-Type-Options"] = XContentTypeOptionsValue;

                // Prevent clickjacking
                headers["X-Frame-Options"] = XFrameOptionsValue;

                // Restrict browser features
                headers["Permissions-Policy"] = PermissionsPolicyValue;

                return Task.CompletedTask;
            });

            // Continue to next middleware
            await next().ConfigureAwait(false);
        });
    }

    /// <summary>
    ///     Configures static file serving for uploaded images.
    ///     Applies aggressive caching since images rarely change.
    /// </summary>
    private static void UseStaticUploads(this WebApplication app)
    {
        // Build the uploads directory path
        var uploadsPath = Path.Combine(app.Environment.WebRootPath, "img", "uploads");

        // Ensure directory exists (create if not present)
        Directory.CreateDirectory(uploadsPath);

        // Configure static file options
        var staticFileOptions = new StaticFileOptions
        {
            // Use physical file provider for this directory
            FileProvider = new PhysicalFileProvider(uploadsPath),

            // URL path prefix
            RequestPath = "/img/uploads",

            // Configure caching headers
            OnPrepareResponse = context =>
            {
                // Cache for 7 days (604800 seconds)
                // "immutable" means the file won't change during cache lifetime
                context.Context.Response.Headers.CacheControl =
                    "public,max-age=604800,immutable";
            }
        };

        app.UseStaticFiles(staticFileOptions);
    }

    /// <summary>
    ///     Helper class for logging database migration operations.
    /// </summary>
    private static class Log
    {
        /// <summary>
        ///     Logs when database migrations are successfully applied.
        /// </summary>
        public static void DatabaseMigrationApplied(ILogger logger)
        {
            logger.LogInformation("Applied database migrations at startup.");
        }

        /// <summary>
        ///     Logs when database migration fails.
        /// </summary>
        public static void DatabaseMigrationFailed(ILogger logger, Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations at startup.");
        }
    }
}