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

        services.AddScoped(sp => new IdentityRedirectManager(
            sp.GetRequiredService<NavigationManager>()));

        services.AddScoped<AuthenticationStateProvider>(sp =>
            new IdentityRevalidatingAuthenticationStateProvider(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<IOptions<IdentityOptions>>()));

        services.AddApplicationServices();

        services.AddScoped<IMediaService>(sp => new MediaService(
            sp.GetRequiredService<IWebHostEnvironment>()));

        services.AddScoped(_ => new MarkdownRenderer());

        AddCachingServices(services);

        AddRedisServices(services);
    }

    private static void AddRedisServices(IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var connectionString = configuration.GetConnectionString("RedisConnectionString");
            if (connectionString == null)
                throw new InvalidOperationException(
                    "Redis connection string 'RedisConnectionString' is not configured.");

            var options = ParseRedisConnectionString(connectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IRedisCacheService, RedisCacheService>();
        services.AddScoped<IDistributedCache>(serviceProvider =>
        {
            var cacheService = serviceProvider.GetRequiredService<IRedisCacheService>();
            return (RedisCacheService)cacheService;
        });

        services.AddSingleton<CacheStampedeProtection>();
    }

    private static ConfigurationOptions ParseRedisConnectionString(string connectionString)
    {
        var options = new ConfigurationOptions();
        string? host = null;
        int? port = null;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length != 2) continue;

            var key = keyValue[0].Trim().ToUpperInvariant();
            var value = keyValue[1].Trim();

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

    private static void AddCachingServices(IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(60)));

            options.AddPolicy("Short", builder => builder.Expire(TimeSpan.FromSeconds(10)));
            options.AddPolicy("Medium", builder => builder.Expire(TimeSpan.FromMinutes(5)));
            options.AddPolicy("Long", builder => builder.Expire(TimeSpan.FromHours(1)));
            options.AddPolicy("Static", builder => builder.Expire(TimeSpan.FromHours(24)));
        });

        services.AddResponseCaching();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();

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

        services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });

        services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });
    }

    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;
    }

    private static void ConfigureAuthenticationRateLimiting(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            var message = "Too many authentication attempts. Please wait a minute and try again.";
            return new ValueTask(context.HttpContext.Response.WriteAsync(message, cancellationToken));
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            BuildAuthenticationRateLimitPartition);
    }

    private static RateLimitPartition<string> BuildAuthenticationRateLimitPartition(HttpContext httpContext)
    {
        var request = httpContext.Request;

        if (!HttpMethods.IsPost(request.Method)) return RateLimitPartition.GetNoLimiter("default");

        var path = request.Path.Value;
        if (path == null) return RateLimitPartition.GetNoLimiter("default");

        var isAuthEndpoint =
            path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);

        if (!isAuthEndpoint) return RateLimitPartition.GetNoLimiter("default");

        var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = $"{clientAddress}:{path}";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            httpContext => AuthRateLimiterOptions);
    }
}