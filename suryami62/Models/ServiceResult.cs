namespace suryami62.Models;

internal sealed class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

internal static class ServiceResult
{
    public static ServiceResult<T> Ok<T>(T data)
    {
        return new ServiceResult<T> { Success = true, Data = data };
    }

    public static ServiceResult<T> Fail<T>(string errorMessage)
    {
        return new ServiceResult<T> { Success = false, ErrorMessage = errorMessage };
    }
}