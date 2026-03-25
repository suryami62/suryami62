#region

using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using suryami62.Application;
using suryami62.Application.Persistence;
using suryami62.Components;
using suryami62.Components.Account;
using suryami62.Data;
using suryami62.Domain.Models;
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
builder.Services.Configure<ForwardedHeadersOptions>(ConfigureForwardedHeaders);
builder.Services.AddRateLimiter(ConfigureAuthenticationRateLimiting);

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
ConfigureExceptionHandling(app);

app.UseForwardedHeaders();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
ConfigureSecurityHeaders(app);
app.UseRateLimiter();

ConfigureStaticUploads(app);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
MapSeoEndpoints(app);
ApplyDatabaseMigrations(app);

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

// Configures trusted forwarded headers when the app is deployed behind a reverse proxy.
static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
}

// Configures a narrow sliding window limiter only for sensitive authentication endpoints.
static void ConfigureAuthenticationRateLimiting(RateLimiterOptions options)
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        return new ValueTask(context.HttpContext.Response.WriteAsync(
            "Too many authentication attempts. Please wait a minute and try again.",
            cancellationToken));
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(BuildAuthenticationRateLimitPartition);
}

// Builds a partition key that scopes throttling by client address and auth route.
static RateLimitPartition<string> BuildAuthenticationRateLimitPartition(HttpContext httpContext)
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
}

// Configures development and production exception handling behavior.
static void ConfigureExceptionHandling(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
        return;
    }

    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}

// Adds baseline response hardening headers to every request.
static void ConfigureSecurityHeaders(WebApplication app)
{
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
}

// Serves uploaded media from the dedicated uploads directory under wwwroot.
static void ConfigureStaticUploads(WebApplication app)
{
    var uploadsPath = Path.Combine(app.Environment.WebRootPath, "img", "uploads");
    Directory.CreateDirectory(uploadsPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/img/uploads"
    });
}

// Registers SEO-related endpoints with dedicated handlers.
static void MapSeoEndpoints(WebApplication app)
{
    app.MapGet("/sitemap.xml", GetSitemapAsync);
    app.MapGet("/robots.txt", GetRobotsAsync);
}

// Generates sitemap.xml when SEO settings enable it and a canonical base URL is configured.
static async Task<IResult> GetSitemapAsync(
    IConfiguration configuration,
    ISettingsRepository settingsRepository,
    IBlogPostService blogPostService)
{
    if (!await IsSeoDocumentEnabledAsync(settingsRepository, "Seo:EnableSitemap").ConfigureAwait(false))
        return Results.NotFound();

    var canonicalBaseUrl = await GetCanonicalBaseUrlAsync(configuration, settingsRepository).ConfigureAwait(false);
    if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("sitemap.xml");

    var (posts, _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);
    return Results.Text(BuildSitemapXml(canonicalBaseUrl, posts), "application/xml; charset=utf-8");
}

// Generates robots.txt when SEO settings enable it and a canonical base URL is configured.
static async Task<IResult> GetRobotsAsync(IConfiguration configuration, ISettingsRepository settingsRepository)
{
    if (!await IsSeoDocumentEnabledAsync(settingsRepository, "Seo:EnableRobots").ConfigureAwait(false))
        return Results.NotFound();

    var canonicalBaseUrl = await GetCanonicalBaseUrlAsync(configuration, settingsRepository).ConfigureAwait(false);
    if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("robots.txt");

    var disallowList =
        await settingsRepository.GetValueAsync("Seo:RobotsDisallow").ConfigureAwait(false) ?? "/Account";

    return Results.Text(BuildRobotsText(canonicalBaseUrl, disallowList), "text/plain; charset=utf-8");
}

// Reads a feature toggle stored in settings and treats any value other than false as enabled.
static async Task<bool> IsSeoDocumentEnabledAsync(ISettingsRepository settingsRepository, string settingKey)
{
    return !string.Equals(
        await settingsRepository.GetValueAsync(settingKey).ConfigureAwait(false),
        "false",
        StringComparison.OrdinalIgnoreCase);
}

// Resolves the canonical base URL from SEO settings first, then from application configuration.
static async Task<string?> GetCanonicalBaseUrlAsync(
    IConfiguration configuration,
    ISettingsRepository settingsRepository)
{
    var baseUrl = await settingsRepository.GetValueAsync("Seo:BaseUrl").ConfigureAwait(false);
    return ResolveCanonicalBaseUrl(baseUrl, configuration["Security:CanonicalBaseUrl"]);
}

// Returns a standardized problem response when canonical URL configuration is missing.
static IResult CreateMissingCanonicalBaseUrlProblem(string fileName)
{
    return Results.Problem(
        $"Configure Seo:BaseUrl or Security:CanonicalBaseUrl before enabling {fileName}.",
        statusCode: StatusCodes.Status503ServiceUnavailable);
}

// Builds the sitemap payload for static pages and published posts.
static string BuildSitemapXml(string canonicalBaseUrl, IEnumerable<BlogPost> posts)
{
    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

    AppendStaticSitemapEntries(sb, canonicalBaseUrl);
    AppendBlogPostSitemapEntries(sb, canonicalBaseUrl, posts);

    sb.AppendLine("</urlset>");
    return sb.ToString();
}

// Adds fixed site routes that should always appear in the sitemap.
static void AppendStaticSitemapEntries(StringBuilder sb, string canonicalBaseUrl)
{
    ReadOnlySpan<string> staticPages = ["/", "/about", "/posts", "/projects"];
    foreach (var page in staticPages)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}{page}</loc>");
        sb.AppendLine("  </url>");
    }
}

// Adds sitemap entries for blog posts including their last modified date.
static void AppendBlogPostSitemapEntries(StringBuilder sb, string canonicalBaseUrl, IEnumerable<BlogPost> posts)
{
    foreach (var post in posts)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}/posts/{post.Slug}</loc>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <lastmod>{post.Date:yyyy-MM-dd}</lastmod>");
        sb.AppendLine("  </url>");
    }
}

// Builds the robots.txt payload from the configured disallow rules and sitemap location.
static string BuildRobotsText(string canonicalBaseUrl, string? disallowList)
{
    var sb = new StringBuilder();
    sb.AppendLine("User-agent: *");
    sb.AppendLine("Allow: /");

    foreach (var disallowEntry in EnumerateRobotsDisallowEntries(disallowList))
        sb.AppendLine(CultureInfo.InvariantCulture, $"Disallow: {disallowEntry}");

    sb.AppendLine(CultureInfo.InvariantCulture, $"Sitemap: {canonicalBaseUrl}/sitemap.xml");
    return sb.ToString();
}

// Normalizes the configured robots disallow rules into non-empty trimmed lines.
static IEnumerable<string> EnumerateRobotsDisallowEntries(string? disallowList)
{
    if (string.IsNullOrWhiteSpace(disallowList)) yield break;

    var lines = disallowList.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    foreach (var line in lines)
    {
        var trimmedLine = line.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedLine)) yield return trimmedLine;
    }
}

// Applies pending EF Core migrations during application startup and logs the outcome.
static void ApplyDatabaseMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();

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