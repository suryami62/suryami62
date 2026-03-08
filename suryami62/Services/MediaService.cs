#region

using System.Globalization;
using System.Text;

#endregion

namespace suryami62.Services;

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

    public Task<List<string>> ListFilesAsync()
    {
        if (!Directory.Exists(_uploadsDirectory)) return Task.FromResult<List<string>>([]);

        var fileNames = new DirectoryInfo(_uploadsDirectory)
            .EnumerateFiles()
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => file.Name)
            .ToList();

        return Task.FromResult(fileNames);
    }

    public async Task<UploadResult> UploadFileAsync(string fileName, string contentType, Stream stream,
        long maxAllowedSize = 5120000)
    {
        try
        {
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName)) return new UploadResult(false, "Invalid file name.");

            var extension = Path.GetExtension(safeFileName);
            if (!AllowedExtensions.Contains(extension))
                return new UploadResult(false, $"Extension '{extension}' is not allowed.");

            safeFileName = BuildUrlEncodedFileName(safeFileName);
            if (string.IsNullOrWhiteSpace(safeFileName)) return new UploadResult(false, "Invalid file name.");

            if (!AllowedContentTypes.Contains(contentType))
                return new UploadResult(false, $"Content type '{contentType}' is not allowed.");

            Directory.CreateDirectory(_uploadsDirectory);

            var destinationPath = Path.Combine(_uploadsDirectory, safeFileName);
            if (File.Exists(destinationPath))
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeFileName);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                safeFileName = $"{nameWithoutExtension}_{timestamp}{extension}";
                destinationPath = Path.Combine(_uploadsDirectory, safeFileName);
            }

            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var copyResult = await CopyToAsyncWithLimit(stream, fileStream, maxAllowedSize).ConfigureAwait(false);

            if (!copyResult.Success)
            {
                try
                {
                    File.Delete(destinationPath);
                }
                catch (IOException)
                {
                    // Best-effort cleanup. The upload still fails.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort cleanup. The upload still fails.
                }

                return new UploadResult(false, copyResult.Message);
            }

            return new UploadResult(true, $"Successfully uploaded {safeFileName}", safeFileName);
        }
        catch (IOException)
        {
            return new UploadResult(false, "An error occurred while uploading the file (I/O error).");
        }
        catch (UnauthorizedAccessException)
        {
            return new UploadResult(false, "An error occurred while uploading the file (Access denied).");
        }
    }

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

            if (char.IsWhiteSpace(c) || c is '-' or '_')
                if (!lastWasDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastWasDash = true;
                }
        }

        return sb.ToString().Trim('-');
    }
}