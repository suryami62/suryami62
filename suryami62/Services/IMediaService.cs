namespace suryami62.Services;

/// <summary>
///     Defines media management operations for uploaded site assets.
/// </summary>
internal interface IMediaService
{
    /// <summary>
    ///     Gets the uploaded file names ordered for display.
    /// </summary>
    /// <returns>The list of uploaded file names.</returns>
    Task<List<string>> ListFilesAsync();

    /// <summary>
    ///     Uploads a file to the media store.
    /// </summary>
    /// <param name="fileName">The original file name supplied by the client.</param>
    /// <param name="contentType">The MIME content type reported by the client.</param>
    /// <param name="stream">The content stream to persist.</param>
    /// <param name="maxAllowedSize">The maximum allowed size in bytes.</param>
    /// <returns>The result of the upload operation.</returns>
    Task<UploadResult> UploadFileAsync(string fileName, string contentType, Stream stream,
        long maxAllowedSize = 5120000);

    /// <summary>
    ///     Deletes an uploaded file.
    /// </summary>
    /// <param name="fileName">The file name to remove.</param>
    /// <returns><see langword="true" /> when the file was deleted; otherwise <see langword="false" />.</returns>
    Task<bool> DeleteFileAsync(string fileName);
}

/// <summary>
///     Represents the result of a media upload attempt.
/// </summary>
/// <param name="Success">Indicates whether the operation succeeded.</param>
/// <param name="Message">The user-facing outcome message.</param>
/// <param name="FileName">The stored file name when the upload succeeds.</param>
internal sealed record UploadResult(bool Success, string Message, string? FileName = null);