#region

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
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

        AddPresentationServices(services);
        AddSecurityServices(services);
        AddPersistenceServices(services, configuration);

        return services;
    }

    private static void AddPresentationServices(IServiceCollection services)
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

            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.AsyncTimeout = 5000;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IRedisCacheService, RedisCacheService>();
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
            options.UseNpgsql(connectionString, npgsqlOptions => { npgsqlOptions.EnableRetryOnFailure(); });
        });

        services.AddInfrastructureServices();
        services.AddDatabaseDeveloperPageExceptionFilter();
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

    private static bool IsSensitiveAuthRequest(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method)) return false;

        var path = request.Path;
        return path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase)
               || path.Equals("/Account/ResendEmailConfirmation", StringComparison.OrdinalIgnoreCase);
    }
}