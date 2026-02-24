#region

using System.Globalization;
using System.Text;
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
builder.Services.AddScoped<UserInfoSettingsStore>();

builder.Services.AddAuthentication(options => { options.DefaultScheme = IdentityConstants.ApplicationScheme; })
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapGet("/sitemap.xml", async (ApplicationDbContext db, IBlogPostService blogPostService, HttpContext context) =>
{
    var enableSitemapSetting =
        await db.Settings.FirstOrDefaultAsync(s => s.Key == "Seo:EnableSitemap").ConfigureAwait(false);
    if (enableSitemapSetting?.Value == "false") return Results.NotFound();

    var baseUrlSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "Seo:BaseUrl").ConfigureAwait(false);
    var baseUrl = baseUrlSetting?.Value;
    if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    baseUrl = baseUrl.TrimEnd('/');

    var (posts, _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);

    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

    ReadOnlySpan<string> staticPages = ["/", "/about", "/posts", "/projects"];
    foreach (var page in staticPages)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{baseUrl}{page}</loc>");
        sb.AppendLine("  </url>");
    }

    foreach (var post in posts)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{baseUrl}/posts/{post.Slug}</loc>");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <lastmod>{post.Date:yyyy-MM-dd}</lastmod>");
        sb.AppendLine("  </url>");
    }

    sb.AppendLine("</urlset>");

    return Results.Text(sb.ToString(), "application/xml; charset=utf-8");
});

app.MapGet("/robots.txt", async (ApplicationDbContext db, HttpContext context) =>
{
    var enableRobotsSetting =
        await db.Settings.FirstOrDefaultAsync(s => s.Key == "Seo:EnableRobots").ConfigureAwait(false);
    if (enableRobotsSetting?.Value == "false") return Results.NotFound();

    var baseUrlSetting = await db.Settings.FirstOrDefaultAsync(s => s.Key == "Seo:BaseUrl").ConfigureAwait(false);
    var baseUrl = baseUrlSetting?.Value;
    if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    baseUrl = baseUrl.TrimEnd('/');

    var disallowSetting =
        await db.Settings.FirstOrDefaultAsync(s => s.Key == "Seo:RobotsDisallow").ConfigureAwait(false);
    var disallowList = disallowSetting?.Value ?? "/Account";

    var sb = new StringBuilder();
    sb.AppendLine("User-agent: *");
    sb.AppendLine("Allow: /");

    if (!string.IsNullOrWhiteSpace(disallowList))
    {
        var lines = disallowList.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines) sb.AppendLine(CultureInfo.InvariantCulture, $"Disallow: {line.Trim()}");
    }

    sb.AppendLine(CultureInfo.InvariantCulture, $"Sitemap: {baseUrl}/sitemap.xml");

    return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
});

app.Run();