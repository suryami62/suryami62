#region

using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using suryami62.Components;
using suryami62.Components.Account;
using suryami62.Data;
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

builder.Services.AddScoped<IBlogPostService>(sp => new BlogPostService(sp.GetRequiredService<ApplicationDbContext>()));
builder.Services.AddScoped<IProjectService>(sp => new ProjectService(sp.GetRequiredService<ApplicationDbContext>()));
builder.Services.AddScoped<IMediaService>(sp => new MediaService(sp.GetRequiredService<IWebHostEnvironment>()));
builder.Services.AddScoped<SeoFilesSettingsStore>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    _ = ApplicationDbContext.Create(new DbContextOptions<ApplicationDbContext>());
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        _ = ApplicationUser.Create();
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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/sitemap.xml", async (HttpContext httpContext, IBlogPostService blogPostService,
        SeoFilesSettingsStore seoFilesSettingsStore, CancellationToken cancellationToken) =>
    {
        var settings = await seoFilesSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.SitemapEnabled) return Results.NotFound();

        var baseUrl = GetBaseUrl(httpContext, settings);
        var urls = new List<(string Loc, DateTime? LastMod)>
        {
            (Combine(baseUrl, "/"), null),
            (Combine(baseUrl, "/about"), null),
            (Combine(baseUrl, "/posts"), null),
            (Combine(baseUrl, "/projects"), null)
        };

        var (posts, _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);
        foreach (var post in posts)
        {
            var slug = Uri.EscapeDataString(post.Slug ?? string.Empty);
            urls.Add((Combine(baseUrl, $"/posts/{slug}"), post.Date));
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset",
                urls.Select(u =>
                {
                    var url = new XElement(ns + "url", new XElement(ns + "loc", u.Loc));
                    if (u.LastMod.HasValue)
                        url.Add(new XElement(ns + "lastmod",
                            u.LastMod.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
                    return url;
                })));

        return Results.Text(doc.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8",
            Encoding.UTF8);
    })
    .AllowAnonymous();

app.MapGet("/robots.txt", async (HttpContext httpContext, SeoFilesSettingsStore seoFilesSettingsStore,
        CancellationToken cancellationToken) =>
    {
        var settings = await seoFilesSettingsStore.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.RobotsEnabled) return Results.NotFound();

        var baseUrl = GetBaseUrl(httpContext, settings);

        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");

        if (settings.DisallowAccount) sb.AppendLine("Disallow: /Account");

        foreach (var line in (settings.AdditionalDisallow ?? string.Empty)
                 .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var path = line.StartsWith('/') ? line : $"/{line}";
            sb.AppendLine(CultureInfo.InvariantCulture, $"Disallow: {path}");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"Sitemap: {Combine(baseUrl, "/sitemap.xml")}");

        return Results.Text(sb.ToString(), "text/plain; charset=utf-8", Encoding.UTF8);
    })
    .AllowAnonymous();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

static string GetBaseUrl(HttpContext httpContext, SeoFilesSettings settings)
{
    if (settings.AutoBaseUrl || string.IsNullOrWhiteSpace(settings.BaseUrl))
        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}".TrimEnd('/');

    return settings.BaseUrl.Trim().TrimEnd('/');
}

static string Combine(string baseUrl, string path)
{
    if (string.IsNullOrWhiteSpace(path) || path == "/") return baseUrl + "/";
    return path.StartsWith('/') ? baseUrl + path : baseUrl + "/" + path;
}