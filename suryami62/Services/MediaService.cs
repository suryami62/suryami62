// ============================================================================
// MEDIA SERVICE
// ============================================================================
// This service handles uploading, storing, and managing image files.
// Used in the admin area to upload images for blog posts and projects.
//
// FEATURES:
// - Validates file types (only images: jpg, png, webp)
// - Validates file size (default max: 5MB)
// - Processes images: resizes if too large, converts to WebP format
// - Creates SEO-friendly file names (URL-safe, no spaces)
// - Stores files in wwwroot/img/uploads for public access
// ============================================================================

#region

using System.Globalization; // For date formatting in file names
using System.Text; // For StringBuilder in file name processing
using SixLabors.ImageSharp; // For image processing (resize, convert)
using SixLabors.ImageSharp.Formats.Webp; // For WebP image format
using SixLabors.ImageSharp.Processing; // For image resize operations

#endregion

namespace suryami62.Services;

/// <summary>
///     Handles uploading, processing, and storing image files on the server.
/// </summary>
internal sealed class MediaService : IMediaService
{
    // Maximum length for the file name (excluding extension)
    private const int MaxSeoFileNameLength = 80;

    // List of allowed file extensions (case-insensitive)
    private static readonly HashSet<string> AllowedExtensions = new(
            StringComparer.OrdinalIgnoreCase) // Ignore case: ".JPG" = ".jpg"
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    // List of allowed MIME content types
    private static readonly HashSet<string> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", // For .jpg and .jpeg files
        "image/png", // For .png files
        "image/webp" // For .webp files
    };

    // The folder where uploaded images are stored
    // Path.Combine builds the correct path for the operating system
    private readonly string _uploadsDirectory;

    /// <summary>
    ///     Creates a new MediaService with the web environment info.
    /// </summary>
    /// <param name="webHostEnvironment">Provides paths like WebRootPath (wwwroot folder).</param>
    public MediaService(IWebHostEnvironment webHostEnvironment)
    {
        // Build the uploads directory path: wwwroot/img/uploads
        _uploadsDirectory = Path.Combine(
            webHostEnvironment.WebRootPath, // wwwroot folder
            "img", // images folder
            "uploads" // uploads subfolder
        );
    }

    /// <inheritdoc />
    public Task<List<string>> ListFilesAsync()
    {
        // Step 1: Check if uploads folder exists
        if (!Directory.Exists(_uploadsDirectory))
            // Return empty list (not null) if directory doesn't exist
            return Task.FromResult(new List<string>());

        // Step 2: Get DirectoryInfo to access files
        var uploadDirectory = new DirectoryInfo(_uploadsDirectory);

        // Step 3: Get all files, sort by date (newest first), select just the names
        var fileNames = uploadDirectory
            .EnumerateFiles() // Get all files in directory
            .OrderByDescending(file => file.LastWriteTimeUtc) // Sort: newest first
            .Select(file => file.Name) // Extract just the file name
            .ToList(); // Convert to List<string>

        return Task.FromResult(fileNames);
    }

    /// <summary>
    ///     Uploads an image file to the server with validation and processing.
    /// </summary>
    public async Task<UploadResult> UploadFileAsync(
        string fileName,
        string contentType,
        Stream stream,
        long maxAllowedSize = 5120000)
    {
        try
        {
            // Step 1: Validate the file (check extension, content type, name)
            var validationResult = ValidateUpload(fileName, contentType);
            if (!validationResult.Success)
                // Validation failed - return the error
                // The ! tells the compiler we know Error is not null when Success is false
                return validationResult.Error!;

            // Step 2: Ensure the uploads directory exists (create if needed)
            Directory.CreateDirectory(_uploadsDirectory);

            // Step 3: Get a unique file name (avoid overwriting existing files)
            // SafeFileName is guaranteed to not be null when Success is true
            var storedFileName = GetUniqueFileName(validationResult.SafeFileName!);
            var destinationPath = Path.Combine(_uploadsDirectory, storedFileName);

            // Step 4: Process the image (resize if needed, convert to WebP)
            var processResult = await ProcessAndSaveImageAsync(
                stream,
                destinationPath,
                maxAllowedSize
            ).ConfigureAwait(false);

            // Step 5: Return success or failure result
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

    /// <summary>
    ///     Deletes an uploaded file from the server.
    /// </summary>
    public Task<bool> DeleteFileAsync(string fileName)
    {
        try
        {
            // Step 1: Get just the file name (no path) for safety
            // Path.GetFileName prevents "../../etc/passwd" style attacks
            var safeFileName = Path.GetFileName(fileName);
            var path = Path.Combine(_uploadsDirectory, safeFileName);

            // Step 2: Check if file exists, then delete it
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

    /// <summary>
    ///     Validates that the file is safe to upload.
    ///     Checks: file name is valid, extension is allowed, content type is allowed.
    /// </summary>
    /// <returns>
    ///     A tuple with three values:
    ///     - Success: true if validation passed
    ///     - SafeFileName: the cleaned file name (only if Success is true)
    ///     - Error: UploadResult with error message (only if Success is false)
    /// </returns>
    private static (bool Success, string? SafeFileName, UploadResult? Error) ValidateUpload(
        string fileName,
        string contentType)
    {
        // Step 1: Extract just the file name (prevents path traversal attacks like "../../../etc/passwd")
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
            return (false, null, new UploadResult(false, "Invalid file name."));

        // Step 2: Check file extension is in our allowed list
        var extension = Path.GetExtension(safeFileName);
        if (!AllowedExtensions.Contains(extension))
            return (false, null, new UploadResult(false, $"Extension '{extension}' is not allowed."));

        // Step 3: Clean the file name to make it URL-safe and SEO-friendly
        var normalizedFileName = BuildUrlEncodedFileName(safeFileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
            return (false, null, new UploadResult(false, "Invalid file name."));

        // Step 4: Verify the content type matches what we expect for this file type
        if (!AllowedContentTypes.Contains(contentType))
            return (false, null, new UploadResult(false, $"Content type '{contentType}' is not allowed."));

        // All checks passed - return the cleaned file name
        return (true, normalizedFileName, null);
    }

    /// <summary>
    ///     Makes a file name unique by adding a timestamp if it already exists.
    /// </summary>
    private string GetUniqueFileName(string safeFileName)
    {
        // Step 1: Check if file already exists
        var destinationPath = Path.Combine(_uploadsDirectory, safeFileName);
        if (!File.Exists(destinationPath))
            // File doesn't exist - use the original name
            return safeFileName;

        // Step 2: File exists - add timestamp to make it unique
        var extension = Path.GetExtension(safeFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(safeFileName);

        // Create timestamp: yyyyMMddHHmmss (e.g., 20260412153045 for April 12, 2026 3:30:45 PM)
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        // Build new name: originalName_timestamp.extension
        return $"{nameWithoutExtension}_{timestamp}{extension}";
    }

    /// <summary>
    ///     Saves a stream to a file with size limit enforcement.
    /// </summary>
    private static async Task<(bool Success, string Message)> SaveFileAsync(
        Stream source,
        string destinationPath,
        long maxAllowedSize)
    {
        // Create the file and open it for writing
        // FileMode.Create = create new or overwrite existing
        // FileAccess.Write = we only need to write, not read
        // FileShare.None = no other program can access while we write
        using var fileStream = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        // Copy data from source to file with size limit
        var copyResult = await CopyToAsyncWithLimit(
            source,
            fileStream,
            maxAllowedSize).ConfigureAwait(false);

        if (copyResult.Success)
            // Copy succeeded - return the result
            return copyResult;

        // Copy failed (e.g., file too large) - clean up partial file
        TryDeletePartialFile(destinationPath);
        return copyResult;
    }

    /// <summary>
    ///     Tries to delete a partially uploaded file if something went wrong.
    ///     "Best-effort" means we try, but don't throw error if it fails.
    /// </summary>
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

    /// <summary>
    ///     Processes an uploaded image: checks size, resizes if too big, converts to WebP.
    /// </summary>
    private static async Task<(bool Success, string Message)> ProcessAndSaveImageAsync(
        Stream source,
        string destinationPath,
        long maxAllowedSize)
    {
        // Step 1: Copy to memory stream so we can check size and process it
        using var memoryStream = new MemoryStream();
        await source.CopyToAsync(memoryStream).ConfigureAwait(false);

        // Step 2: Check if file is too large
        if (memoryStream.Length > maxAllowedSize)
            return (false, $"File is too large. Max allowed size is {maxAllowedSize} bytes.");

        // Step 3: Reset position to beginning so ImageSharp can read it
        memoryStream.Position = 0;

        try
        {
            // Step 4: Load the image using ImageSharp library
            using var image = await Image.LoadAsync(memoryStream).ConfigureAwait(false);

            // Step 5: Resize if image is too large (max 1920px on longest side)
            const int maxDimension = 1920;
            if (image.Width > maxDimension || image.Height > maxDimension)
                // Mutate means "modify the image"
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max, // Keep aspect ratio
                    Size = new Size(maxDimension, maxDimension) // Max width and height
                }));

            // Step 6: Auto-orient based on EXIF data (phones often rotate images)
            image.Mutate(x => x.AutoOrient());

            // Step 7: Determine output path (convert to WebP if not already)
            var extension = Path.GetExtension(destinationPath).ToUpperInvariant();
            string outputPath;

            if (extension != ".WEBP")
                // Convert to WebP for better compression and smaller file size
                outputPath = Path.ChangeExtension(destinationPath, ".webp");
            else
                // Already WebP - keep original path
                outputPath = destinationPath;

            // Step 8: Configure WebP encoder settings
            var webpEncoder = new WebpEncoder
            {
                Quality = 85, // 85% quality (good balance of size vs. quality)
                FileFormat = WebpFileFormatType.Lossy // Lossy compression (smaller than lossless)
            };

            // Step 9: Save the processed image
            await image.SaveAsync(outputPath, webpEncoder).ConfigureAwait(false);

            // Step 10: If we converted to WebP, delete the original file if it exists
            if (outputPath != destinationPath && File.Exists(destinationPath)) File.Delete(destinationPath);

            return (true, string.Empty);
        }
        catch (UnknownImageFormatException)
        {
            // ImageSharp couldn't load the image (not a valid image or unsupported format)
            // Fall back to saving the file without processing
            memoryStream.Position = 0;
            return await SaveFileAsync(memoryStream, destinationPath, maxAllowedSize).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Copies data from source stream to destination stream with size limit.
    ///     Reads in chunks (80KB buffer) to avoid loading entire large files into memory.
    /// </summary>
    private static async Task<(bool Success, string Message)> CopyToAsyncWithLimit(
        Stream source,
        Stream destination,
        long maxAllowedSize)
    {
        // Validate max size parameter
        if (maxAllowedSize <= 0) return (false, "Max allowed size must be greater than 0.");

        // Create an 80KB buffer (81920 bytes)
        // This is a good size for I/O operations - not too small (inefficient)
        // not too big (wastes memory)
        var buffer = new byte[81920];
        long totalBytesCopied = 0;

        // Keep reading until stream is empty (bytesRead == 0)
        while (true)
        {
            // Read up to buffer.Length bytes from source
            var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);

            // 0 bytes means we've reached the end of the stream
            if (bytesRead == 0) break;

            // Add to running total and check if we've exceeded the limit
            totalBytesCopied = totalBytesCopied + bytesRead;
            if (totalBytesCopied > maxAllowedSize)
                return (false, $"File is too large. Max allowed size is {maxAllowedSize} bytes.");

            // Write the bytes we just read to the destination
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
        }

        return (true, string.Empty);
    }

    /// <summary>
    ///     Converts a file name to a safe, URL-friendly format.
    ///     Example: "My Photo (1).jpg" → "my-photo-1.jpg"
    /// </summary>
    private static string BuildUrlEncodedFileName(string fileName)
    {
        // Step 1: Get just the name without extension and trim whitespace
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).Trim();
        if (string.IsNullOrWhiteSpace(nameWithoutExtension)) return string.Empty;

        // Step 2: Convert to SEO-friendly slug format
        var seoFriendlyName = BuildSeoFriendlyName(nameWithoutExtension);

        // Step 3: Truncate if too long (max 80 chars)
        if (seoFriendlyName.Length > MaxSeoFileNameLength)
            // Substring(0, maxLength) gets characters from index 0 to maxLength
            seoFriendlyName = seoFriendlyName.Substring(0, MaxSeoFileNameLength).Trim('-');

        // Step 4: If name became empty after processing, use "image" as default
        if (string.IsNullOrWhiteSpace(seoFriendlyName)) seoFriendlyName = "image";

        // Step 5: Get the original extension
        var extension = Path.GetExtension(fileName);

        // Step 6: URL-encode the name (replaces spaces and special chars with %20, etc.)
        var encodedName = Uri.EscapeDataString(seoFriendlyName);

        // Step 7: Combine encoded name with original extension
        return encodedName + extension;
    }

    /// <summary>
    ///     Converts a raw file name into a "slug" format for URLs.
    ///     Removes special characters, converts to lowercase, replaces spaces with dashes.
    ///     Example: "Hello World & Stuff.jpg" → "hello-world-stuff"
    /// </summary>
    private static string BuildSeoFriendlyName(string input)
    {
        // Step 1: Normalize Unicode characters
        // This handles accented characters: "café" becomes "cafe"
        // FormD = Decomposed form (splits accents from letters)
        var normalized = input.Normalize(NormalizationForm.FormD);

        // Step 2: StringBuilder is efficient for building strings character-by-character
        var result = new StringBuilder(normalized.Length);
        var lastCharacterWasDash = false;

        // Step 3: Process each character one at a time
        foreach (var character in normalized)
        {
            // Get the Unicode category of this character
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            // Skip "non-spacing marks" - these are accents that attach to letters
            // We already normalized so the letter and accent are separate - skip the accent
            if (category == UnicodeCategory.NonSpacingMark) continue;

            // Keep letters and numbers - convert to lowercase
            if (char.IsLetterOrDigit(character))
            {
                result.Append(char.ToLowerInvariant(character));
                lastCharacterWasDash = false; // Reset dash tracker
                continue;
            }

            // If we see a space or dash/underscore, and we haven't just added a dash,
            // and we're not at the start (sb.Length > 0), add a single dash
            var isSeparator = char.IsWhiteSpace(character) || character == '-' || character == '_';
            if (isSeparator && !lastCharacterWasDash && result.Length > 0)
            {
                result.Append('-');
                lastCharacterWasDash = true; // Mark that we just added a dash
            }
            // All other characters (special symbols like !@#$%) are skipped/removed
        }

        // Step 4: Remove any trailing dashes and return
        return result.ToString().Trim('-');
    }
}