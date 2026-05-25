#region

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using suryami62.Components;
using suryami62.Components.Account;
using suryami62.Data;

#endregion

namespace suryami62.Startup;

internal static class WebApplicationExtensions
{
    private const string ContentSecurityPolicyValue =
        "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none; " +
        "img-src 'self' data: https:; font-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; connect-src 'self' ws: wss:; form-action 'self'";

    private const string ReferrerPolicyValue = "strict-origin-when-cross-origin";

    private const string XContentTypeOptionsValue = "nosniff";

    private const string XFrameOptionsValue = "DENY";

    private const string PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    public static WebApplication UseWebStartupPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ConfigureExceptionHandling(app);

        app.UseForwardedHeaders();

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        app.UseHttpsRedirection();

        app.UseResponseCompression();

        app.UseSecurityHeaders();

        app.UseRateLimiter();

        app.UseResponseCaching();

        app.UseOutputCache();

        app.UseStaticUploads();

        app.UseAntiforgery();

        return app;
    }

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
        }
        else
        {
            app.UseExceptionHandler("/Error", true);
            app.UseHsts(); // Enforce HTTPS
        }
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

        var staticFileOptions = new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadsPath),

            RequestPath = "/img/uploads",

            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers.CacheControl =
                    "public,max-age=604800,immutable";
            }
        };

        app.UseStaticFiles(staticFileOptions);
    }

    private static class Log
    {
        public static void DatabaseMigrationApplied(ILogger logger)
        {
            logger.LogInformation("Applied database migrations at startup.");
        }

        public static void DatabaseMigrationFailed(ILogger logger, Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying database migrations at startup.");
        }
    }
}