// ============================================================================
// SEO ENDPOINT ROUTE BUILDER EXTENSIONS
// ============================================================================
// This file generates SEO (Search Engine Optimization) files:
// - sitemap.xml: Tells search engines about all pages on the site
// - robots.txt: Tells search engine crawlers which pages they can access
//
// WHAT IS A SITEMAP?
// An XML file listing all URLs on your website. Search engines like Google
// use this to discover and index your pages faster.
//
// WHAT IS ROBOTS.TXT?
// A text file that tells web crawlers which pages they can or cannot access.
// It also points to the sitemap location.
//
// EXAMPLE SITEMAP.XML:
// <?xml version="1.0" encoding="UTF-8"?>
// <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
//   <url>
//     <loc>https://example.com/</loc>
//   </url>
//   <url>
//     <loc>https://example.com/posts/hello-world</loc>
//     <lastmod>2024-01-15</lastmod>
//   </url>
// </urlset>
//
// EXAMPLE ROBOTS.TXT:
// User-agent: *
// Allow: /
// Disallow: /Account
// Sitemap: https://example.com/sitemap.xml
// ============================================================================

#region

using System.Globalization;
using System.Text;
using suryami62.Application.Persistence;
using suryami62.Domain.Models;
using suryami62.Services;

#endregion

namespace suryami62.Startup;

/// <summary>
///     Extension methods for mapping SEO endpoints (sitemap.xml and robots.txt).
/// </summary>
internal static class SeoEndpointRouteBuilderExtensions
{
    /// <summary>
    ///     Maps the SEO endpoints (/sitemap.xml and /robots.txt).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static IEndpointConventionBuilder MapSeoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Create an endpoint group (empty prefix means root level)
        var seoGroup = endpoints.MapGroup(string.Empty);

        // Map GET /sitemap.xml to the GetSitemapAsync method
        seoGroup.MapGet("/sitemap.xml", GetSitemapAsync);

        // Map GET /robots.txt to the GetRobotsAsync method
        seoGroup.MapGet("/robots.txt", GetRobotsAsync);

        return seoGroup;
    }

    /// <summary>
    ///     Handles requests for /sitemap.xml.
    ///     Returns an XML file listing all pages on the site.
    /// </summary>
    private static async Task<IResult> GetSitemapAsync(
        IConfiguration configuration,
        ISettingsRepository settingsRepository,
        IBlogPostService blogPostService)
    {
        // Step 1: Check if sitemap is enabled in settings
        var sitemapEnabled =
            await IsSeoDocumentEnabledAsync(settingsRepository, "Seo:EnableSitemap").ConfigureAwait(false);
        if (!sitemapEnabled) return Results.NotFound();

        // Step 2: Get the base URL (required for building full URLs)
        var canonicalBaseUrl = await GetCanonicalBaseUrlAsync(configuration, settingsRepository).ConfigureAwait(false);
        if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("sitemap.xml");

        // Step 3: Get all blog posts to include in sitemap
        (IEnumerable<BlogPost> posts, var _) = await blogPostService.GetPostsAsync().ConfigureAwait(false);

        // Step 4: Build and return the XML sitemap
        var sitemapXml = BuildSitemapXml(canonicalBaseUrl, posts);
        return Results.Text(sitemapXml, "application/xml; charset=utf-8");
    }

    /// <summary>
    ///     Handles requests for /robots.txt.
    ///     Returns a text file with crawling instructions for search engines.
    /// </summary>
    private static async Task<IResult> GetRobotsAsync(
        IConfiguration configuration,
        ISettingsRepository settingsRepository)
    {
        // Step 1: Check if robots.txt is enabled in settings
        var robotsEnabled =
            await IsSeoDocumentEnabledAsync(settingsRepository, "Seo:EnableRobots").ConfigureAwait(false);
        if (!robotsEnabled) return Results.NotFound();

        // Step 2: Get the base URL (required for sitemap reference)
        var canonicalBaseUrl = await GetCanonicalBaseUrlAsync(configuration, settingsRepository).ConfigureAwait(false);
        if (canonicalBaseUrl is null) return CreateMissingCanonicalBaseUrlProblem("robots.txt");

        // Step 3: Get the list of paths to disallow (default: /Account)
        var disallowList = await settingsRepository.GetValueAsync("Seo:RobotsDisallow").ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(disallowList)) disallowList = "/Account";

        // Step 4: Build and return the robots.txt content
        var robotsText = BuildRobotsText(canonicalBaseUrl, disallowList);
        return Results.Text(robotsText, "text/plain; charset=utf-8");
    }

    /// <summary>
    ///     Checks if an SEO document is enabled in settings.
    ///     Returns true unless explicitly set to "false".
    /// </summary>
    private static async Task<bool> IsSeoDocumentEnabledAsync(ISettingsRepository settingsRepository, string settingKey)
    {
        var storedValue = await settingsRepository.GetValueAsync(settingKey).ConfigureAwait(false);

        // Check if the value is explicitly "false" (case-insensitive)
        var isDisabled = string.Equals(storedValue, "false", StringComparison.OrdinalIgnoreCase);

        // Return true if NOT disabled (enabled by default)
        return !isDisabled;
    }

    /// <summary>
    ///     Gets the canonical base URL from settings or configuration.
    ///     This is the root URL of the website (e.g., https://example.com).
    /// </summary>
    private static async Task<string?> GetCanonicalBaseUrlAsync(
        IConfiguration configuration,
        ISettingsRepository settingsRepository)
    {
        // Try to get from settings first
        var settingsBaseUrl = await settingsRepository.GetValueAsync("Seo:BaseUrl").ConfigureAwait(false);

        // Fall back to configuration if settings is empty
        var configuredBaseUrl = configuration["Security:CanonicalBaseUrl"];

        return ResolveCanonicalBaseUrl(settingsBaseUrl, configuredBaseUrl);
    }

    /// <summary>
    ///     Validates and normalizes the base URL.
    ///     Returns null if the URL is invalid or not http/https.
    /// </summary>
    private static string? ResolveCanonicalBaseUrl(string? settingsBaseUrl, string? configuredBaseUrl)
    {
        // Choose the candidate URL (settings takes priority)
        string? candidate;
        if (string.IsNullOrWhiteSpace(settingsBaseUrl))
            candidate = configuredBaseUrl;
        else
            candidate = settingsBaseUrl;

        // Validate the URL
        if (string.IsNullOrWhiteSpace(candidate)) return null;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return null;

        if (uri.Scheme != "http" && uri.Scheme != "https") return null;

        // Remove trailing slash for consistency
        return candidate.TrimEnd('/');
    }

    /// <summary>
    ///     Creates an error response when the base URL is not configured.
    /// </summary>
    private static IResult CreateMissingCanonicalBaseUrlProblem(string fileName)
    {
        var message = $"Configure Seo:BaseUrl or Security:CanonicalBaseUrl before enabling {fileName}.";
        return Results.Problem(message, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    ///     Builds the sitemap.xml content as a string.
    /// </summary>
    private static string BuildSitemapXml(string canonicalBaseUrl, IEnumerable<BlogPost> posts)
    {
        // StringBuilder is more efficient than string concatenation in loops
        var sb = new StringBuilder(4096); // Pre-allocate ~4KB

        // XML declaration
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

        // Root element with namespace
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Add static pages (home, about, posts list, projects)
        AppendStaticSitemapEntries(sb, canonicalBaseUrl);

        // Add dynamic pages (individual blog posts)
        AppendBlogPostSitemapEntries(sb, canonicalBaseUrl, posts);

        // Close root element
        sb.AppendLine("</urlset>");

        return sb.ToString();
    }

    /// <summary>
    ///     Adds static page URLs to the sitemap.
    /// </summary>
    private static void AppendStaticSitemapEntries(StringBuilder sb, string canonicalBaseUrl)
    {
        // Array of static page paths
        var staticPages = new[] { "/", "/about", "/posts", "/projects" };

        foreach (var page in staticPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>{canonicalBaseUrl}{page}</loc>");
            sb.AppendLine("  </url>");
        }
    }

    /// <summary>
    ///     Adds blog post URLs to the sitemap with last modified dates.
    /// </summary>
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

    /// <summary>
    ///     Builds the robots.txt content as a string.
    /// </summary>
    private static string BuildRobotsText(string canonicalBaseUrl, string? disallowList)
    {
        var sb = new StringBuilder(512); // Pre-allocate ~512 bytes

        // Allow all user agents (crawlers)
        sb.AppendLine("User-agent: *");

        // Allow all pages by default
        sb.AppendLine("Allow: /");

        // Add disallow entries (paths crawlers should not access)
        foreach (var disallowEntry in EnumerateRobotsDisallowEntries(disallowList))
            sb.AppendLine(CultureInfo.InvariantCulture, $"Disallow: {disallowEntry}");

        // Point to the sitemap location
        sb.AppendLine(CultureInfo.InvariantCulture, $"Sitemap: {canonicalBaseUrl}/sitemap.xml");

        return sb.ToString();
    }

    /// <summary>
    ///     Parses the disallow list string into individual entries.
    ///     Handles comma or newline separated values.
    /// </summary>
    private static IEnumerable<string> EnumerateRobotsDisallowEntries(string? disallowList)
    {
        // If empty, return nothing
        if (string.IsNullOrWhiteSpace(disallowList)) yield break;

        // Split by newlines and carriage returns
        // Using a static array to avoid CA1861 warning (constant array in loop)
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