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
///     Registers the web-layer services required by the application host.
/// </summary>
internal static class WebServiceCollectionExtensions
{
    private static readonly SlidingWindowRateLimiterOptions AuthRateLimiterOptions = new()
    {
        PermitLimit = 5,
        Window = TimeSpan.FromMinutes(1),
        SegmentsPerWindow = 6,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
        AutoReplenishment = true
    };

    /// <summary>
    ///     Adds the presentation, security, and persistence services used by the web project.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddWebApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddPresentationServices(services, configuration);
        AddSecurityServices(services);
        AddPersistenceServices(services, configuration);

        return services;
    }

    private static void AddPresentationServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddCascadingAuthenticationState();
        services.AddScoped(sp => new IdentityRedirectManager(sp.GetRequiredService<NavigationManager>()));
        services.AddScoped<AuthenticationStateProvider>(sp => new IdentityRevalidatingAuthenticationStateProvider(
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IOptions<IdentityOptions>>()));

        services.AddApplicationServices();
        services.AddScoped<IMediaService>(sp => new MediaService(sp.GetRequiredService<IWebHostEnvironment>()));
        services.AddScoped(_ => new MarkdownRenderer());

        // Add caching services (memory + distributed + output)
        AddCachingServices(services);

        // Add Redis distributed cache
        AddRedisServices(services);
    }

    private static void AddRedisServices(IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("RedisConnectionString") ??
                                   throw new InvalidOperationException(
                                       "Redis connection string 'RedisConnectionString' is not configured.");

            var options = ParseRedisConnectionString(connectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IRedisCacheService, RedisCacheService>();
    }

    /// <summary>
    ///     Parses a Redis connection string in the format:
    ///     Host=[HOST];Port=[PORT];Username=[USERNAME];Password=[PASSWORD]
    ///     into StackExchange.Redis ConfigurationOptions.
    /// </summary>
    private static ConfigurationOptions ParseRedisConnectionString(string connectionString)
    {
        var options = new ConfigurationOptions();
        string? host = null;
        int? port = null;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim().ToUpperInvariant();
            var value = keyValue[1].Trim();

            switch (key)
            {
                case "HOST" when !string.IsNullOrEmpty(value):
                    host = value;
                    break;
                case "PORT" when int.TryParse(value, out var parsedPort):
                    port = parsedPort;
                    break;
                case "USERNAME":
                    options.User = value;
                    break;
                case "PASSWORD":
                    options.Password = value;
                    break;
                case "DEFAULTDATABASE" when int.TryParse(value, out var db):
                    options.DefaultDatabase = db;
                    break;
                case "ABORTCONNECTFAIL" or "ABORTCONNECT" when bool.TryParse(value, out var abortConnect):
                    options.AbortOnConnectFail = abortConnect;
                    break;
                case "CONNECTTIMEOUT" when int.TryParse(value, out var connectTimeout):
                    options.ConnectTimeout = connectTimeout;
                    break;
                case "SYNCTIMEOUT" when int.TryParse(value, out var syncTimeout):
                    options.SyncTimeout = syncTimeout;
                    break;
                case "CONNECTRETRY" when int.TryParse(value, out var connectRetry):
                    options.ConnectRetry = connectRetry;
                    break;
                case "ssl" or "usessl" when bool.TryParse(value, out var ssl):
                    options.Ssl = ssl;
                    break;
            }
        }

        if (string.IsNullOrEmpty(host))
            throw new InvalidOperationException(
                "No Redis endpoint specified. Connection string must contain at least a 'Host' parameter.");

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

        services.AddSingleton<IEmailSender<ApplicationUser>>(_ => new IdentityNoOpEmailSender());
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
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                ["application/javascript", "application/css", "text/css", "text/javascript", "image/svg+xml"]);
        });

        services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });

        services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });
    }

    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost;
    }

    private static void ConfigureAuthenticationRateLimiting(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            return new ValueTask(context.HttpContext.Response.WriteAsync(
                "Too many authentication attempts. Please wait a minute and try again.",
                cancellationToken));
        };

        options.GlobalLimiter =
            PartitionedRateLimiter.Create<HttpContext, string>(BuildAuthenticationRateLimitPartition);
    }

    private static RateLimitPartition<string> BuildAuthenticationRateLimitPartition(HttpContext httpContext)
    {
        var request = httpContext.Request;

        // Inline path check to avoid method call overhead in hot path
        if (!HttpMethods.IsPost(request.Method))
            return RateLimitPartition.GetNoLimiter("default");

        var path = request.Path.Value;
        if (path is null)
            return RateLimitPartition.GetNoLimiter("default");

        // Fast path check using Span-based comparison
        var pathSpan = path.AsSpan();
        if (!pathSpan.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) &&
            !pathSpan.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase) &&
            !pathSpan.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase) &&
            !pathSpan.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase))
            return RateLimitPartition.GetNoLimiter("default");

        var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{clientAddress}:{path}";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => AuthRateLimiterOptions);
    }
}