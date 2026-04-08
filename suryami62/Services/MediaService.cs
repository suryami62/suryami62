#region

using System.Globalization;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

#endregion

namespace suryami62.Services;

/// <summary>
///     Manages uploaded image files stored under the site's public uploads directory.
/// </summary>
/// <remarks>
///     This service backs the admin media workflow by validating incoming files, normalizing public file names,
///     and exposing the upload directory as a simple image library for the rest of the site.
/// </remarks>
internal sealed class MediaService(IWebHostEnvironment webHostEnvironment) : IMediaService
{
    private const int MaxSeoFileNameLength = 80;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly string _uploadsDirectory = Path.Combine(webHostEnvironment.WebRootPath, "img", "uploads");

    /// <inheritdoc />
    public Task<List<string>> ListFilesAsync()
    {
        if (!Directory.Exists(_uploadsDirectory)) return Task.FromResult<List<string>>([]);

        var uploadDirectory = new DirectoryInfo(_uploadsDirectory);
        var fileNames = uploadDirectory
            .EnumerateFiles()
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => file.Name)
            .ToList();

        return Task.FromResult(fileNames);
    }

    /// <inheritdoc />
    public async Task<UploadResult> UploadFileAsync(string fileName, string contentType, Stream stream,
        long maxAllowedSize = 5120000)
    {
        try
        {
            var validationResult = ValidateUpload(fileName, contentType);
            if (!validationResult.Success) return validationResult.Error!;

            Directory.CreateDirectory(_uploadsDirectory);

            var storedFileName = GetUniqueFileName(validationResult.SafeFileName!);
            var destinationPath = Path.Combine(_uploadsDirectory, storedFileName);

            // Process and optimize image before saving
            var processResult = await ProcessAndSaveImageAsync(stream, destinationPath, maxAllowedSize, contentType)
                .ConfigureAwait(false);

            return processResult.Success
                ? new UploadResult(true, $"Successfully uploaded {storedFileName}", storedFileName)
                : new UploadResult(false, processResult.Message);
        }
        catch (IOException)
        {
            return new UploadResult(false, "An error occurred while uploading the file (I/O error).");
        }
        catch (UnauthorizedAccessException)
        {
            return new UploadResult(false, "An error occurred while uploading the file (Access denied).");
        }
        catch (InvalidOperationException ex)
        {
            return new UploadResult(false, $"Image processing error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var safeFileName = Path.GetFileName(fileName);
            var path = Path.Combine(_uploadsDirectory, safeFileName);

            if (File.Exists(path))
            {
                File.Delete(path);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (IOException)
        {
            return Task.FromResult(false);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    private static (bool Success, string? SafeFileName, UploadResult? Error) ValidateUpload(
        string fileName,
        string contentType)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return (false, null, new UploadResult(false, "Invalid file name."));

        var extension = Path.GetExtension(safeFileName);
        if (!AllowedExtensions.Contains(extension))
            return (false, null, new UploadResult(false, $"Extension '{extension}' is not allowed."));

        var normalizedFileName = BuildUrlEncodedFileName(safeFileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
            return (false, null, new UploadResult(false, "Invalid file name."));

        if (!AllowedContentTypes.Contains(contentType))
            return (false, null, new UploadResult(false, $"Content type '{contentType}' is not allowed."));

        return (true, normalizedFileName, null);
    }

    private string GetUniqueFileName(string safeFileName)
    {
        var destinationPath = Path.Combine(_uploadsDirectory, safeFileName);
        if (!File.Exists(destinationPath)) return safeFileName;

        var extension = Path.GetExtension(safeFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        return $"{nameWithoutExtension}_{timestamp}{extension}";
    }

    private static async Task<(bool Success, string Message)> SaveFileAsync(
        Stream source,
        string destinationPath,
        long maxAllowedSize)
    {
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var copyResult = await CopyToAsyncWithLimit(source, fileStream, maxAllowedSize).ConfigureAwait(false);
        if (copyResult.Success) return copyResult;

        TryDeletePartialFile(destinationPath);
        return copyResult;
    }

    private static void TryDeletePartialFile(string destinationPath)
    {
        try
        {
            File.Delete(destinationPath);
        }
        catch (IOException)
        {
            // Cleanup is best-effort; preserve the original upload failure when deletion also fails.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup is best-effort; preserve the original upload failure when deletion also fails.
        }
    }

    /// <summary>
    ///     Processes the uploaded image by resizing if too large and converting to WebP for optimal compression.
    /// </summary>
    private static async Task<(bool Success, string Message)> ProcessAndSaveImageAsync(
        Stream source,
        string destinationPath,
        long maxAllowedSize,
        string contentType)
    {
        // Check size limit first using a memory buffer
        using var memoryStream = new MemoryStream();
        await source.CopyToAsync(memoryStream).ConfigureAwait(false);

        if (memoryStream.Length > maxAllowedSize)
            return (false, $"File is too large. Max allowed size is {maxAllowedSize} bytes.");

        memoryStream.Position = 0;

        try
        {
            using var image = await Image.LoadAsync(memoryStream).ConfigureAwait(false);

            // Resize if image is too large (max 1920px on longest side)
            const int maxDimension = 1920;
            if (image.Width > maxDimension || image.Height > maxDimension)
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimension, maxDimension)
                }));

            // Auto-orient based on EXIF data
            image.Mutate(x => x.AutoOrient());

            // Determine output format based on content type
            var extension = Path.GetExtension(destinationPath).ToUpperInvariant();

            // Convert to WebP for better compression (unless already WebP)
            string outputPath;
            if (extension != ".WEBP")
                outputPath = Path.ChangeExtension(destinationPath, ".webp");
            else
                outputPath = destinationPath;

            // Save as WebP with quality 85 (good balance between quality and size)
            var webpEncoder = new WebpEncoder
            {
                Quality = 85,
                FileFormat = WebpFileFormatType.Lossy
            };

            await image.SaveAsync(outputPath, webpEncoder).ConfigureAwait(false);

            // If we converted to WebP, delete the original path reference
            if (outputPath != destinationPath && File.Exists(destinationPath)) File.Delete(destinationPath);

            return (true, string.Empty);
        }
        catch (UnknownImageFormatException)
        {
            // If image processing fails, fall back to direct save
            memoryStream.Position = 0;
            return await SaveFileAsync(memoryStream, destinationPath, maxAllowedSize).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Streams an uploaded file to disk while enforcing the configured size limit.
    /// </summary>
    /// <param name="source">The source stream.</param>
    /// <param name="destination">The destination stream.</param>
    /// <param name="maxAllowedSize">The maximum number of bytes allowed.</param>
    /// <returns>A tuple describing whether the copy succeeded and an optional message.</returns>
    private static async Task<(bool Success, string Message)> CopyToAsyncWithLimit(
        Stream source,
        Stream destination,
        long maxAllowedSize)
    {
        if (maxAllowedSize <= 0) return (false, "Max allowed size must be greater than 0.");

        var buffer = new byte[81920];
        long totalBytesCopied = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (bytesRead == 0) break;

            totalBytesCopied += bytesRead;
            if (totalBytesCopied > maxAllowedSize)
                return (false, $"File is too large. Max allowed size is {maxAllowedSize} bytes.");

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
        }

        return (true, string.Empty);
    }

    /// <summary>
    ///     Normalizes an uploaded file name into a stable, URL-safe asset name for public storage.
    /// </summary>
    /// <param name="fileName">The original file name.</param>
    /// <returns>The sanitized file name, or an empty string when the input is unusable.</returns>
    private static string BuildUrlEncodedFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (string.IsNullOrWhiteSpace(nameWithoutExtension)) return string.Empty;

        var seoFriendlyName = BuildSeoFriendlyName(nameWithoutExtension);

        if (seoFriendlyName.Length > MaxSeoFileNameLength)
            seoFriendlyName = seoFriendlyName[..MaxSeoFileNameLength].Trim('-');

        if (string.IsNullOrWhiteSpace(seoFriendlyName)) seoFriendlyName = "image";

        var extension = Path.GetExtension(fileName);
        var encodedName = Uri.EscapeDataString(seoFriendlyName);

        return $"{encodedName}{extension}";
    }

    /// <summary>
    ///     Converts a free-form file name into a short slug that remains readable in public asset URLs.
    /// </summary>
    /// <param name="input">The raw file name without extension.</param>
    /// <returns>The generated slug.</returns>
    private static string BuildSeoFriendlyName(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                lastWasDash = false;
                continue;
            }

            if ((char.IsWhiteSpace(c) || c is '-' or '_') && !lastWasDash && sb.Length > 0)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }

        return sb.ToString().Trim('-');
    }
}