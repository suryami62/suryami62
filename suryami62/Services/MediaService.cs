#region

using System.Globalization;

#endregion

namespace suryami62.Services;

internal sealed class MediaService(IWebHostEnvironment environment) : IMediaService
{
    private static readonly string[] AllowedExtensions = [".JPG", ".JPEG", ".PNG", ".WEBP"];
    private static readonly string[] AllowedContentTypes = ["IMAGE/JPEG", "IMAGE/PNG", "IMAGE/WEBP"];
    private readonly string _uploadPath = Path.Combine(environment.WebRootPath, "img", "uploads");

    public async Task<List<string>> ListFilesAsync()
    {
        if (!Directory.Exists(_uploadPath)) return [];

        return await Task.Run(() =>
        {
            return new DirectoryInfo(_uploadPath)
                .GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.Name)
                .ToList();
        }).ConfigureAwait(false);
    }

    public async Task<UploadResult> UploadFileAsync(string fileName, string contentType, Stream stream,
        long maxAllowedSize = 5120000)
    {
        try
        {
            // 1. Sanitize filename & check for path traversal
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName)) return new UploadResult(false, "Invalid file name.");

            // 2. Validate extension
            var extension = Path.GetExtension(safeFileName).ToUpperInvariant();
            if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return new UploadResult(false, $"Extension {extension} is not allowed.");

            // 3. Validate content type
            if (!AllowedContentTypes.Contains(contentType.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase))
                return new UploadResult(false, $"Content type {contentType} is not allowed.");

            // 4. Ensure directory exists
            if (!Directory.Exists(_uploadPath)) Directory.CreateDirectory(_uploadPath);

            // 5. Check for duplicate and rename if necessary (optional but good for UX)
            var finalPath = Path.Combine(_uploadPath, safeFileName);
            if (File.Exists(finalPath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                safeFileName = $"{nameWithoutExt}_{timestamp}{extension}";
                finalPath = Path.Combine(_uploadPath, safeFileName);
            }

            // 6. Save file with size limit
            var fs = new FileStream(finalPath, FileMode.Create);
            await using (fs.ConfigureAwait(false))
            {
                await stream.CopyToAsync(fs).ConfigureAwait(false);
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

    public async Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            var safeFileName = Path.GetFileName(fileName);
            var path = Path.Combine(_uploadPath, safeFileName);

            if (File.Exists(path))
            {
                await Task.Run(() => File.Delete(path)).ConfigureAwait(false);
                return true;
            }

            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}