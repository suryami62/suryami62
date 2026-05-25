#region

using System.Globalization;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

#endregion

namespace suryami62.Services;

internal sealed class MediaService : IMediaService
{
    private const int MaxSeoFileNameLength = 80;

    private static readonly HashSet<string> AllowedExtensions = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly string _uploadsDirectory;

    public MediaService(IWebHostEnvironment webHostEnvironment)
    {
        _uploadsDirectory = Path.Combine(
            webHostEnvironment.WebRootPath,
            "img",
            "uploads"
        );
    }

    public Task<List<string>> ListFilesAsync()
    {
        if (!Directory.Exists(_uploadsDirectory))
            return Task.FromResult(new List<string>());

        var uploadDirectory = new DirectoryInfo(_uploadsDirectory);

        var fileNames = uploadDirectory
            .EnumerateFiles()
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => file.Name)
            .ToList();

        return Task.FromResult(fileNames);
    }

    public async Task<UploadResult> UploadFileAsync(
        string fileName,
        string contentType,
        Stream stream,
        long maxAllowedSize = 5120000)
    {
        try
        {
            var validationResult = ValidateUpload(fileName, contentType);
            if (!validationResult.Success)
                return validationResult.Error!;

            Directory.CreateDirectory(_uploadsDirectory);

            var storedFileName = GetUniqueFileName(validationResult.SafeFileName!);
            var destinationPath = Path.Combine(_uploadsDirectory, storedFileName);

            var processResult = await ProcessAndSaveImageAsync(
                stream,
                destinationPath,
                maxAllowedSize
            ).ConfigureAwait(false);

            if (processResult.Success)
                return new UploadResult(
                    true,
                    $"Successfully uploaded {storedFileName}",
                    storedFileName
                );

            return new UploadResult(
                false,
                processResult.Message
            );
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
        if (!File.Exists(destinationPath))
            return safeFileName;

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
        using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        var copyResult = await CopyToAsyncWithLimit(
            source,
            fileStream,
            maxAllowedSize).ConfigureAwait(false);

        if (copyResult.Success)
            return copyResult;

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
            // Couldn't delete (maybe file is locked).
            // This is "best-effort" cleanup - the original upload error
            // is more important, so we ignore this and let the caller
            // handle the actual upload failure message.
        }
        catch (UnauthorizedAccessException)
        {
            // Couldn't delete (no permission).
            // Same as above - best-effort cleanup, ignore the error.
        }
    }

    private static async Task<(bool Success, string Message)> ProcessAndSaveImageAsync(
        Stream source,
        string destinationPath,
        long maxAllowedSize)
    {
        using var memoryStream = new MemoryStream();
        await source.CopyToAsync(memoryStream).ConfigureAwait(false);

        if (memoryStream.Length > maxAllowedSize)
            return (false, $"File is too large. Max allowed size is {maxAllowedSize} bytes.");

        memoryStream.Position = 0;

        try
        {
            using var image = await Image.LoadAsync(memoryStream).ConfigureAwait(false);

            const int maxDimension = 1920;
            if (image.Width > maxDimension || image.Height > maxDimension)
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimension, maxDimension)
                }));

            image.Mutate(x => x.AutoOrient());

            var extension = Path.GetExtension(destinationPath).ToUpperInvariant();
            string outputPath;

            if (extension != ".WEBP")
                outputPath = Path.ChangeExtension(destinationPath, ".webp");
            else
                outputPath = destinationPath;

            var webpEncoder = new WebpEncoder
            {
                Quality = 85,
                FileFormat = WebpFileFormatType.Lossy
            };

            await image.SaveAsync(outputPath, webpEncoder).ConfigureAwait(false);

            if (outputPath != destinationPath && File.Exists(destinationPath)) File.Delete(destinationPath);

            return (true, string.Empty);
        }
        catch (UnknownImageFormatException)
        {
            memoryStream.Position = 0;
            return await SaveFileAsync(memoryStream, destinationPath, maxAllowedSize).ConfigureAwait(false);
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

            totalBytesCopied = totalBytesCopied + bytesRead;
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
            seoFriendlyName = seoFriendlyName.Substring(0, MaxSeoFileNameLength).Trim('-');

        if (string.IsNullOrWhiteSpace(seoFriendlyName)) seoFriendlyName = "image";

        var extension = Path.GetExtension(fileName);

        var encodedName = Uri.EscapeDataString(seoFriendlyName);

        return encodedName + extension;
    }

    private static string BuildSeoFriendlyName(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);

        var result = new StringBuilder(normalized.Length);
        var lastCharacterWasDash = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
                lastCharacterWasDash = false;
                continue;
            }

            var isSeparator = char.IsWhiteSpace(character) || character == '-' || character == '_';
            if (isSeparator && !lastCharacterWasDash && result.Length > 0)
            {
                result.Append('-');
                lastCharacterWasDash = true;
            }
        }

        return result.ToString().Trim('-');
    }
}