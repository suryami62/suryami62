#region

using System.Globalization;
using System.Text;
using suryami62.Application.Persistence;
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
        ISettingsRepository settingsRepository,
        IBlogPostService blogPostService)
    {
        var sitemapEnabled =
            await IsSeoDocumentEnabledAsync(settingsRepository, "Seo:EnableSitemap").ConfigureAwait(false);
        if (!sitemapEnabled) return Results.NotFound();

        var canonicalBaseUrl = await GetCanonicalBaseUrlAsync(configuration, settingsRepository).ConfigureAwait(false);
        if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("sitemap.xml");

        (IEnumerable<BlogPost> posts, _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);

        var sitemapXml = BuildSitemapXml(canonicalBaseUrl, posts);
        return Results.Text(sitemapXml, "application/xml; charset=utf-8");
    }

    private static async Task<IResult> GetRobotsAsync(
        IConfiguration configuration,
        ISettingsRepository settingsRepository)
    {
        var robotsEnabled =
            await IsSeoDocumentEnabledAsync(settingsRepository, "Seo:EnableRobots").ConfigureAwait(false);
        if (!robotsEnabled) return Results.NotFound();

        var canonicalBaseUrl = await GetCanonicalBaseUrlAsync(configuration, settingsRepository).ConfigureAwait(false);
        if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("robots.txt");

        var disallowList = await settingsRepository.GetValueAsync("Seo:RobotsDisallow").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(disallowList)) disallowList = "/Account";

        var robotsText = BuildRobotsText(canonicalBaseUrl, disallowList);
        return Results.Text(robotsText, "text/plain; charset=utf-8");
    }

    private static async Task<bool> IsSeoDocumentEnabledAsync(ISettingsRepository settingsRepository, string settingKey)
    {
        var storedValue = await settingsRepository.GetValueAsync(settingKey).ConfigureAwait(false);

        var isDisabled = string.Equals(storedValue, "false", StringComparison.OrdinalIgnoreCase);

        return !isDisabled;
    }

    private static async Task<string?> GetCanonicalBaseUrlAsync(
        IConfiguration configuration,
        ISettingsRepository settingsRepository)
    {
        var settingsBaseUrl = await settingsRepository.GetValueAsync("Seo:BaseUrl").ConfigureAwait(false);

        var configuredBaseUrl = configuration["Security:CanonicalBaseUrl"];

        return ResolveCanonicalBaseUrl(settingsBaseUrl, configuredBaseUrl);
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