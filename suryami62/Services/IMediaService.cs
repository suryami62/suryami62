namespace suryami62.Services;

internal interface IMediaService
{
    Task<List<string>> ListFilesAsync();

    Task<UploadResult> UploadFileAsync(string fileName, string contentType, Stream stream,
        long maxAllowedSize = 5120000);

    Task<bool> DeleteFileAsync(string fileName);
}

internal sealed record UploadResult(bool Success, string Message, string? FileName = null);