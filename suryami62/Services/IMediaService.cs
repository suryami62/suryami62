// ============================================================================
// MEDIA SERVICE INTERFACE
// ============================================================================
// This interface defines the contract (what methods are available) for
// managing uploaded files (images for blog posts, projects, etc.)
//
// WHAT IS AN INTERFACE?
// An interface is like a contract that says "any class implementing this
// MUST have these methods with these exact signatures."
//
// This allows us to:
// 1. Write code that depends on IMediaService without caring about the
//    specific implementation (MediaService)
// 2. Easily test by creating a fake/mock implementation
// 3. Swap implementations later if needed
// ============================================================================

namespace suryami62.Services;

/// <summary>
///     Defines the operations available for managing uploaded media files.
/// </summary>
internal interface IMediaService
{
    /// <summary>
    ///     Gets a list of all uploaded files, sorted by newest first.
    /// </summary>
    /// <returns>List of file names as strings.</returns>
    Task<List<string>> ListFilesAsync();

    /// <summary>
    ///     Uploads a new image file to the server.
    /// </summary>
    /// <param name="fileName">The original file name from the user's computer.</param>
    /// <param name="contentType">The MIME type (e.g., "image/jpeg", "image/png").</param>
    /// <param name="stream">The file data as a stream of bytes.</param>
    /// <param name="maxAllowedSize">Maximum file size in bytes (default: 5MB = 5,120,000 bytes).</param>
    /// <returns>An UploadResult indicating success/failure and details.</returns>
    Task<UploadResult> UploadFileAsync(
        string fileName,
        string contentType,
        Stream stream,
        long maxAllowedSize = 5120000);

    /// <summary>
    ///     Deletes an uploaded file from the server.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <returns>True if file was deleted, false if not found or error.</returns>
    Task<bool> DeleteFileAsync(string fileName);
}

/// <summary>
///     Represents the result of a media upload attempt.
///     This record (simple data class) holds:
///     - Success: Did the upload work?
///     - Message: What happened (success message or error)
///     - FileName: The final stored file name (only if Success = true)
/// </summary>
internal sealed record UploadResult(bool Success, string Message, string? FileName = null);