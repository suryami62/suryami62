// ============================================================================
// WEB SERVICE COLLECTION EXTENSIONS
// ============================================================================
// This file contains extension methods that register all services needed
// by the web application. "Services" are objects that provide functionality
// like database access, caching, authentication, etc.
//
// WHAT IS DEPENDENCY INJECTION?
// Instead of creating objects with "new", we register them here and ASP.NET
// automatically provides them when needed. This is called "Dependency Injection"
// and makes testing and maintenance easier.
//
// SERVICE LIFETIMES:
// - Singleton: One instance for entire application (e.g., Redis connection)
// - Scoped: One instance per HTTP request (e.g., database context)
// - Transient: New instance every time (e.g., MarkdownRenderer)
// ============================================================================

#region

using System.IO.Compression;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using suryami62.Application;
using suryami62.Components.Account;
using suryami62.Data;
using suryami62.Infrastructure;
using suryami62.Security;
using suryami62.Services;

#endregion

namespace suryami62.Startup;

/// <summary>
///     Extension methods for registering application services.
/// </summary>
internal static class WebServiceCollectionExtensions
{
    // Rate limiting options for authentication endpoints
    // Prevents brute force attacks by limiting login attempts
    private static readonly SlidingWindowRateLimiterOptions AuthRateLimiterOptions = new()
    {
        PermitLimit = 5, // Allow 5 requests
        Window = TimeSpan.FromMinutes(1), // Per 1 minute window
        SegmentsPerWindow = 6, // Divide window into 6 segments (10 seconds each)
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0, // Don't queue requests, reject immediately if limit reached
        AutoReplenishment = true // Automatically refill permits over time
    };

    /// <summary>
    ///     Main entry point for registering all application services.
    ///     Called from Program.cs.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">Application configuration (appsettings.json).</param>
    public static IServiceCollection AddWebApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Validate parameters - throw error if null
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Add services in three categories:
        AddPresentationServices(services, configuration); // UI, Razor Components
        AddSecurityServices(services); // Authentication, authorization
        AddPersistenceServices(services, configuration); // Database, caching

        return services;
    }

    /// <summary>
    ///     Registers presentation layer services (UI components, rendering).
    /// </summary>
    private static void AddPresentationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add Razor Components (Blazor) with interactive server-side rendering
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Add authentication state management
        // CascadingAuthenticationState makes auth info available to all components
        services.AddCascadingAuthenticationState();

        // Register Identity redirect manager (handles login/logout redirects)
        services.AddScoped(sp => new IdentityRedirectManager(
            sp.GetRequiredService<NavigationManager>()));

        // Register authentication state provider (tracks if user is logged in)
        services.AddScoped<AuthenticationStateProvider>(sp =>
            new IdentityRevalidatingAuthenticationStateProvider(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOptions<IdentityOptions>>()));

        // Register application layer services (business logic)
        services.AddApplicationServices();

        // Register media service for file uploads
        services.AddScoped<IMediaService>(sp => new MediaService(
            sp.GetRequiredService<IWebHostEnvironment>()));

        // Register Markdown renderer (transient = new instance each time)
        services.AddScoped(_ => new MarkdownRenderer());

        // Add caching services (memory cache, distributed cache, output cache)
        AddCachingServices(services);

        // Add Redis distributed cache
        AddRedisServices(services);
    }

    /// <summary>
    ///     Registers Redis caching services.
    /// </summary>
    private static void AddRedisServices(IServiceCollection services)
    {
        // Register Redis connection as Singleton (one connection for entire app)
        // This follows Microsoft's best practices for Redis connections
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            // Get configuration to read connection string
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Read Redis connection string from configuration
            var connectionString = configuration.GetConnectionString("RedisConnectionString");
            if (connectionString == null)
                throw new InvalidOperationException(
                    "Redis connection string 'RedisConnectionString' is not configured.");

            // Parse the connection string into Redis options
            var options = ParseRedisConnectionString(connectionString);
            options.AbortOnConnectFail = false; // Don't crash if Redis is temporarily unavailable

            // Create and return the Redis connection
            return ConnectionMultiplexer.Connect(options);
        });

        // Register cache service as Scoped (one per HTTP request)
        // Register as both interfaces so it can be used either way
        services.AddScoped<IRedisCacheService, RedisCacheService>();
        services.AddScoped<IDistributedCache>(serviceProvider =>
        {
            // Get the RedisCacheService and cast it to IDistributedCache
            var cacheService = serviceProvider.GetRequiredService<IRedisCacheService>();
            return (RedisCacheService)cacheService;
        });

        // Register cache stampede protection
        // This prevents "thundering herd" - when many requests hit a missing cache key simultaneously
        services.AddSingleton<CacheStampedeProtection>();
    }

    /// <summary>
    ///     Parses a Redis connection string into ConfigurationOptions.
    ///     Expected format:
    ///     Host=myserver;Port=6379;Username=myuser;Password=mypass;DefaultDatabase=0
    /// </summary>
    private static ConfigurationOptions ParseRedisConnectionString(string connectionString)
    {
        // Create options object to populate
        var options = new ConfigurationOptions();
        string? host = null;
        int? port = null;

        // Split connection string by semicolons into key=value pairs
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            // Split each part into key and value
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2) continue; // Skip malformed parts

            var key = keyValue[0].Trim().ToUpperInvariant();
            var value = keyValue[1].Trim();

            // Process each known configuration option
            switch (key)
            {
                case "HOST":
                    if (!string.IsNullOrEmpty(value)) host = value;
                    break;

                case "PORT":
                    if (int.TryParse(value, out var parsedPort)) port = parsedPort;
                    break;

                case "USERNAME":
                    options.User = value;
                    break;

                case "PASSWORD":
                    options.Password = value;
                    break;

                case "DEFAULTDATABASE":
                    if (int.TryParse(value, out var db)) options.DefaultDatabase = db;
                    break;

                case "ABORTCONNECTFAIL":
                case "ABORTCONNECT":
                    if (bool.TryParse(value, out var abortConnect)) options.AbortOnConnectFail = abortConnect;
                    break;

                case "CONNECTTIMEOUT":
                    if (int.TryParse(value, out var connectTimeout)) options.ConnectTimeout = connectTimeout;
                    break;
                case "SYNCTIMEOUT":
                    if (int.TryParse(value, out var syncTimeout)) options.SyncTimeout = syncTimeout;
                    break;

                case "CONNECTRETRY":
                    if (int.TryParse(value, out var connectRetry)) options.ConnectRetry = connectRetry;
                    break;

                case "SSL":
                case "USESSL":
                    if (bool.TryParse(value, out var useSsl)) options.Ssl = useSsl;
                    break;
            }
        }

        // Validate that we have at least a host
        if (string.IsNullOrEmpty(host))
            throw new InvalidOperationException(
                "No Redis endpoint specified. Connection string must contain at least a 'Host' parameter.");

        // Add the endpoint with or without port
        if (port.HasValue)
            options.EndPoints.Add(new DnsEndPoint(host, port.Value));
        else
            options.EndPoints.Add(host);

        return options;
    }

    private static void AddSecurityServices(IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(ConfigureForwardedHeaders);
        services.AddRateLimiter(ConfigureAuthenticationRateLimiting);

        services.AddSingleton<IAuthorizationHandler>(sp =>
            new AdminAccessHandler(sp.GetRequiredService<IConfiguration>()));
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminAccessPolicy.Name, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(AdminAccessRequirement.Instance);
            });

        services.AddAuthentication(options => { options.DefaultScheme = IdentityConstants.ApplicationScheme; })
            .AddIdentityCookies();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Register a no-op email sender (emails are not actually sent)
        // In production, replace this with a real email service (SendGrid, AWS SES, etc.)
        services.AddSingleton<IEmailSender<ApplicationUser>>(serviceProvider =>
            new IdentityNoOpEmailSender());
    }

    private static void AddPersistenceServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(3);
                npgsqlOptions.CommandTimeout(30);
            });
        });

        services.AddInfrastructureServices();
        services.AddDatabaseDeveloperPageExceptionFilter();
    }

    /// <summary>
    ///     Adds caching services: In-Memory Cache, Output Cache, and Response Caching.
    /// </summary>
    private static void AddCachingServices(IServiceCollection services)
    {
        // Add memory cache for frequently accessed, rarely-changing data
        services.AddMemoryCache();

        // Add output caching for Blazor pages (better than response caching for UI apps)
        services.AddOutputCache(options =>
        {
            // Base policy: 60 seconds for most content
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));

            // Named policies for different cache durations
            options.AddPolicy("Short", builder => builder.Expire(TimeSpan.FromSeconds(10)));
            options.AddPolicy("Medium", builder => builder.Expire(TimeSpan.FromMinutes(5)));
            options.AddPolicy("Long", builder => builder.Expire(TimeSpan.FromHours(1)));
            options.AddPolicy("Static", builder => builder.Expire(TimeSpan.FromHours(24)));
        });

        // Add response caching middleware for static files and API responses
        services.AddResponseCaching();

        // Add response compression for text-based content
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

            // Combine default MIME types with additional ones we want compressed
            var additionalMimeTypes = new[]
            {
                "application/javascript",
                "application/css",
                "text/css",
                "text/javascript",
                "image/svg+xml"
            };
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(additionalMimeTypes);
        });

        // Configure Brotli compression to use optimal compression level
        services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });

        // Configure Gzip compression to use optimal compression level
        services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });
    }

    /// <summary>
    ///     Configures forwarded headers for proxy/load balancer scenarios.
    ///     When running behind a reverse proxy (like Nginx or AWS ALB), the proxy
    ///     forwards client information in headers. This tells ASP.NET to use them.
    /// </summary>
    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | // Original client IP address
            ForwardedHeaders.XForwardedProto | // Original protocol (http/https)
            ForwardedHeaders.XForwardedHost; // Original host header
    }

    /// <summary>
    ///     Configures rate limiting for authentication endpoints.
    ///     Prevents brute force attacks by limiting login/register attempts.
    /// </summary>
    private static void ConfigureAuthenticationRateLimiting(RateLimiterOptions options)
    {
        // Return 429 Too Many Requests when limit is exceeded
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Custom message when request is rejected
        options.OnRejected = (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            var message = "Too many authentication attempts. Please wait a minute and try again.";
            return new ValueTask(context.HttpContext.Response.WriteAsync(message, cancellationToken));
        };

        // Create the rate limiter using our custom partition logic
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            BuildAuthenticationRateLimitPartition);
    }

    /// <summary>
    ///     Determines if a request should be rate limited and creates the appropriate partition.
    ///     Only POST requests to authentication endpoints are rate limited.
    /// </summary>
    private static RateLimitPartition<string> BuildAuthenticationRateLimitPartition(HttpContext httpContext)
    {
        var request = httpContext.Request;

        // Only rate limit POST requests (form submissions)
        if (!HttpMethods.IsPost(request.Method)) return RateLimitPartition.GetNoLimiter("default");

        // Get the request path
        var path = request.Path.Value;
        if (path == null) return RateLimitPartition.GetNoLimiter("default");

        // Check if this is an authentication endpoint we want to protect
        var isAuthEndpoint =
            path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);

        if (!isAuthEndpoint) return RateLimitPartition.GetNoLimiter("default");

        // Create a partition key based on client IP and path
        // This means each IP gets its own rate limit counter per endpoint
        var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{clientAddress}:{path}";

        // Return a sliding window rate limiter for this partition
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            httpContext => AuthRateLimiterOptions);
    }
}