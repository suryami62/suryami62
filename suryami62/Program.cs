#region

using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using suryami62.Application;
using suryami62.Application.Persistence;
using suryami62.Components;
using suryami62.Components.Account;
using suryami62.Data;
using suryami62.Infrastructure;
using suryami62.Security;
using suryami62.Services;

#endregion

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped(sp => new IdentityRedirectManager(sp.GetRequiredService<NavigationManager>()));
builder.Services.AddScoped<AuthenticationStateProvider>(sp => new IdentityRevalidatingAuthenticationStateProvider(
    sp.GetRequiredService<ILoggerFactory>(),
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IOptions<IdentityOptions>>()));

builder.Services.AddApplicationServices();
builder.Services.AddScoped<IMediaService>(sp => new MediaService(sp.GetRequiredService<IWebHostEnvironment>()));
builder.Services.AddScoped(_ => new MarkdownRenderer());
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        return new ValueTask(context.HttpContext.Response.WriteAsync(
            "Too many authentication attempts. Please wait a minute and try again.",
            cancellationToken));
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (!IsSensitiveAuthRequest(httpContext.Request)) return RateLimitPartition.GetNoLimiter("default");

        var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{clientAddress}:{httpContext.Request.Path.Value}";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddSingleton<IAuthorizationHandler>(sp =>
    new AdminAccessHandler(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminAccessPolicy.Name, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(AdminAccessRequirement.Instance);
    });

builder.Services.AddAuthentication(options => { options.DefaultScheme = IdentityConstants.ApplicationScheme; })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions => { npgsqlOptions.EnableRetryOnFailure(); });
});
builder.Services.AddInfrastructureServices();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>>(_ => new IdentityNoOpEmailSender());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] = BuildContentSecurityPolicy();
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });

    await next().ConfigureAwait(false);
});
app.UseRateLimiter();

var uploadsPath = Path.Combine(app.Environment.WebRootPath, "img", "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/img/uploads"
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapGet("/sitemap.xml",
    async (IConfiguration configuration, ISettingsRepository settingsRepository, IBlogPostService blogPostService) =>
    {
        var enableSitemap =
            await settingsRepository.GetValueAsync("Seo:EnableSitemap").ConfigureAwait(false);
        if (enableSitemap == "false") return Results.NotFound();

        var baseUrl = await settingsRepository.GetValueAsync("Seo:BaseUrl").ConfigureAwait(false);
        var canonicalBaseUrl = ResolveCanonicalBaseUrl(baseUrl, configuration["Security:CanonicalBaseUrl"]);
        if (canonicalBaseUrl is null)
            return Results.Problem(
                "Configure Seo:BaseUrl or Security:CanonicalBaseUrl before enabling sitemap.xml.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        var (posts, _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        ReadOnlySpan<string> staticPages = ["/", "/about", "/posts", "/projects"];
        foreach (var page in staticPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}{page}</loc>");
            sb.AppendLine("  </url>");
        }

        foreach (var post in posts)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}/posts/{post.Slug}</loc>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <lastmod>{post.Date:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Results.Text(sb.ToString(), "application/xml; charset=utf-8");
    });

app.MapGet("/robots.txt", async (IConfiguration configuration, ISettingsRepository settingsRepository) =>
{
    var enableRobots =
        await settingsRepository.GetValueAsync("Seo:EnableRobots").ConfigureAwait(false);
    if (enableRobots == "false") return Results.NotFound();

    var baseUrl = await settingsRepository.GetValueAsync("Seo:BaseUrl").ConfigureAwait(false);
    var canonicalBaseUrl = ResolveCanonicalBaseUrl(baseUrl, configuration["Security:CanonicalBaseUrl"]);
    if (canonicalBaseUrl is null)
        return Results.Problem(
            "Configure Seo:BaseUrl or Security:CanonicalBaseUrl before enabling robots.txt.",
            statusCode: StatusCodes.Status503ServiceUnavailable);

    var disallowList =
        await settingsRepository.GetValueAsync("Seo:RobotsDisallow").ConfigureAwait(false) ?? "/Account";

    var sb = new StringBuilder();
    sb.AppendLine("User-agent: *");
    sb.AppendLine("Allow: /");

    if (!string.IsNullOrWhiteSpace(disallowList))
    {
        var lines = disallowList.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines) sb.AppendLine(CultureInfo.InvariantCulture, $"Disallow: {line.Trim()}");
    }

    sb.AppendLine(CultureInfo.InvariantCulture, $"Sitemap: {canonicalBaseUrl}/sitemap.xml");

    return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
});

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        var logMigrationsApplied = LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1001, nameof(Program)),
            "Applied database migrations at startup.");
        logMigrationsApplied(logger, null);
    }
    catch (Exception ex)
    {
        var logMigrationError = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1002, nameof(Program)),
            "An error occurred while applying database migrations at startup.");
        logMigrationError(logger, ex);
        throw;
    }
}

static bool IsSensitiveAuthRequest(HttpRequest request)
{
    if (!HttpMethods.IsPost(request.Method)) return false;

    return request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
           request.Path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase) ||
           request.Path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
           request.Path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);
}

static string BuildContentSecurityPolicy()
{
    return string.Join("; ",
        "default-src 'self'",
        "base-uri 'self'",
        "object-src 'none'",
        "frame-ancestors 'none'",
        "img-src 'self' data: https:",
        "font-src 'self' data:",
        "style-src 'self' 'unsafe-inline'",
        "script-src 'self' 'unsafe-inline'",
        "connect-src 'self' ws: wss:",
        "form-action 'self'");
}

static string? ResolveCanonicalBaseUrl(string? settingsBaseUrl, string? configuredBaseUrl)
{
    var candidate = string.IsNullOrWhiteSpace(settingsBaseUrl)
        ? configuredBaseUrl
        : settingsBaseUrl;

    if (string.IsNullOrWhiteSpace(candidate)) return null;

    if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return null;

    if (uri.Scheme is not ("http" or "https")) return null;

    return candidate.TrimEnd('/');
}

app.Run();