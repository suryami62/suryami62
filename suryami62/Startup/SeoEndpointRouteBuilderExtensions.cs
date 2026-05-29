#region

using System.Globalization;
using System.Text;
using suryami62.Domain.Models;
using suryami62.Services;

#endregion

namespace suryami62.Startup;

internal static class SeoEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var seoGroup = endpoints.MapGroup(string.Empty);

        seoGroup.MapGet("/sitemap.xml", GetSitemapAsync);

        seoGroup.MapGet("/robots.txt", GetRobotsAsync);

        return seoGroup;
    }

    private static async Task<IResult> GetSitemapAsync(
        IConfiguration configuration,
        SeoSettingsStore seoSettingsStore,
        IBlogPostService blogPostService)
    {
        var seoSettings = await seoSettingsStore.GetAsync().ConfigureAwait(false);

        if (!seoSettings.EnableSitemap) return Results.NotFound();

        var canonicalBaseUrl = GetCanonicalBaseUrl(configuration, seoSettings);
        if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("sitemap.xml");

        (IEnumerable<BlogPost> posts, _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);

        var sitemapXml = BuildSitemapXml(canonicalBaseUrl, posts);
        return Results.Text(sitemapXml, "application/xml; charset=utf-8");
    }

    private static async Task<IResult> GetRobotsAsync(
        IConfiguration configuration,
        SeoSettingsStore seoSettingsStore)
    {
        var seoSettings = await seoSettingsStore.GetAsync().ConfigureAwait(false);

        if (!seoSettings.EnableRobots) return Results.NotFound();

        var canonicalBaseUrl = GetCanonicalBaseUrl(configuration, seoSettings);
        if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("robots.txt");

        var disallowList = seoSettings.RobotsDisallow;
        if (string.IsNullOrWhiteSpace(disallowList)) disallowList = "/Account";

        var robotsText = BuildRobotsText(canonicalBaseUrl, disallowList);
        return Results.Text(robotsText, "text/plain; charset=utf-8");
    }

    private static string? GetCanonicalBaseUrl(
        IConfiguration configuration,
        SeoSettings seoSettings)
    {
        var configuredBaseUrl = configuration["Security:CanonicalBaseUrl"];

        return ResolveCanonicalBaseUrl(seoSettings.BaseUrl, configuredBaseUrl);
    }

    private static string? ResolveCanonicalBaseUrl(string? settingsBaseUrl, string? configuredBaseUrl)
    {
        string? candidate;
        if (string.IsNullOrWhiteSpace(settingsBaseUrl))
            candidate = configuredBaseUrl;
        else
            candidate = settingsBaseUrl;

        if (string.IsNullOrWhiteSpace(candidate)) return null;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return null;

        if (uri.Scheme != "http" && uri.Scheme != "https") return null;

        return candidate.TrimEnd('/');
    }

    private static IResult CreateMissingCanonicalBaseUrlProblem(string fileName)
    {
        var message = $"Configure Seo:BaseUrl or Security:CanonicalBaseUrl before enabling {fileName}.";
        return Results.Problem(message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static string BuildSitemapXml(string canonicalBaseUrl, IEnumerable<BlogPost> posts)
    {
        var sb = new StringBuilder(4096);

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

        sb.AppendLine("<?xml-stylesheet type=\"text/xsl\" href=\"/sitemap.xsl\"?>");

        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        AppendStaticSitemapEntries(sb, canonicalBaseUrl);

        AppendBlogPostSitemapEntries(sb, canonicalBaseUrl, posts);

        sb.AppendLine("</urlset>");

        return sb.ToString();
    }

    private static void AppendStaticSitemapEntries(StringBuilder sb, string canonicalBaseUrl)
    {
        var staticPages = new[] { "/", "/about", "/posts", "/projects" };

        foreach (var page in staticPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}{page}</loc>");
            sb.AppendLine("  </url>");
        }
    }

    private static void AppendBlogPostSitemapEntries(
        StringBuilder sb,
        string canonicalBaseUrl,
        IEnumerable<BlogPost> posts)
    {
        foreach (var post in posts)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}/posts/{post.Slug}</loc>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <lastmod>{post.Date:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }
    }

    private static string BuildRobotsText(string canonicalBaseUrl, string? disallowList)
    {
        var sb = new StringBuilder(512);

        sb.AppendLine("User-agent: *");

        sb.AppendLine("Allow: /");

        foreach (var disallowEntry in EnumerateRobotsDisallowEntries(disallowList))
            sb.AppendLine(CultureInfo.InvariantCulture, $"Disallow: {disallowEntry}");

        sb.AppendLine(CultureInfo.InvariantCulture, $"Sitemap: {canonicalBaseUrl}/sitemap.xml");

        return sb.ToString();
    }

    private static IEnumerable<string> EnumerateRobotsDisallowEntries(string? disallowList)
    {
        if (string.IsNullOrWhiteSpace(disallowList)) yield break;

        static char[] GetSeparators()
        {
            return new[] { '\r', '\n' };
        }

        var separators = GetSeparators();
        var lines = disallowList.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed)) yield return trimmed;
        }
    }
}