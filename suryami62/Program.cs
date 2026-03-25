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

// Registers the Blazor UI stack, Identity services, application layers, and request hardening
// dependencies used by the site before the host is built.
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

// Composes the runtime middleware and endpoint pipeline for forwarded headers, security,
// static assets, interactive components, account endpoints, SEO documents, and startup migrations.
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

// Registers supporting account endpoints that the Blazor Identity pages post back to,
// including logout and personal-data export flows under /Account.
app.MapAdditionalIdentityEndpoints();
MapSeoEndpoints(app);
ApplyDatabaseMigrations(app);

// Identifies POST requests that can change authentication state and therefore need
// stricter rate limiting than the rest of the site.
static bool IsSensitiveAuthRequest(HttpRequest request)
{
    if (!HttpMethods.IsPost(request.Method)) return false;

    return request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
           request.Path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase) ||
           request.Path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
           request.Path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);
}

// Builds the baseline Content-Security-Policy header used for all responses, allowing
// the site's local assets and the websocket connections required by interactive Blazor.
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

    // Trusts the forwarded host, scheme, and client IP headers emitted by a fronting proxy
    // so generated URLs, redirects, and request metadata reflect the public endpoint.
static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
}

// Applies a dedicated sliding-window limiter to sensitive account POST endpoints while
// leaving the rest of the application unthrottled by the global limiter.
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

// Keys authentication throttling by remote address and requested auth path so repeated
// attempts against one flow do not consume the budget for unrelated requests.
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

// Uses detailed migration diagnostics in development and the hardened error pipeline in
// non-development environments, including HSTS for production deployments.
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

// Appends the baseline hardening headers for every response after downstream middleware
// has prepared the status code and payload.
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

// Ensures the uploads directory exists and exposes it through the same static file
// pipeline as the rest of the site's public web assets.
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

// Maps the generated SEO documents so search engines can fetch sitemap.xml and robots.txt
// directly from the application when those features are enabled.
static void MapSeoEndpoints(WebApplication app)
{
    app.MapGet("/sitemap.xml", GetSitemapAsync);
    app.MapGet("/robots.txt", GetRobotsAsync);
}

// Produces sitemap.xml from the site's static routes and published blog posts only when
// the feature is enabled and a canonical base URL can be resolved.
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

// Produces robots.txt from the stored disallow rules and sitemap location only when the
// robots feature is enabled and the canonical base URL is available.
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

// Reads an SEO feature toggle from persisted settings and treats missing values as enabled
// so the default behavior remains permissive unless the administrator explicitly disables it.
static async Task<bool> IsSeoDocumentEnabledAsync(ISettingsRepository settingsRepository, string settingKey)
{
    return !string.Equals(
        await settingsRepository.GetValueAsync(settingKey).ConfigureAwait(false),
        "false",
        StringComparison.OrdinalIgnoreCase);
}

    // Resolves the canonical site URL by preferring the editable SEO setting and falling back
    // to static application configuration when no site-specific override is stored.
static async Task<string?> GetCanonicalBaseUrlAsync(
    IConfiguration configuration,
    ISettingsRepository settingsRepository)
{
    var baseUrl = await settingsRepository.GetValueAsync("Seo:BaseUrl").ConfigureAwait(false);
    return ResolveCanonicalBaseUrl(baseUrl, configuration["Security:CanonicalBaseUrl"]);
}

// Returns a consistent operational error when an SEO document is enabled without a usable
// canonical base URL, making the misconfiguration visible to administrators and monitors.
static IResult CreateMissingCanonicalBaseUrlProblem(string fileName)
{
    return Results.Problem(
        $"Configure Seo:BaseUrl or Security:CanonicalBaseUrl before enabling {fileName}.",
        statusCode: StatusCodes.Status503ServiceUnavailable);
}

    // Builds the final sitemap XML document by combining fixed site routes with the current
    // set of blog post URLs returned by the application layer.
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

// Appends the site's always-available public routes so crawlers can discover the core
// landing, about, posts, and projects pages even without dynamic content.
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

// Appends one sitemap entry per blog post, including the post slug and last-modified date
// so search engines can detect content updates efficiently.
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

// Builds robots.txt with a permissive default allow rule, the configured disallow entries,
// and a sitemap reference anchored to the resolved canonical base URL.
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

// Splits the stored robots disallow text into normalized non-empty entries so admins can
// maintain one or many paths using simple multiline text in settings.
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

// Applies any pending EF Core migrations during startup so deployed environments converge
// on the expected schema before the site begins serving requests.
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

// Chooses the first non-empty canonical base URL candidate, validates that it is an HTTP(S)
// absolute URI, and trims the trailing slash for stable downstream URL composition.
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