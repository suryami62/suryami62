namespace suryami62.Application.Persistence;

public sealed class ConcurrencyConflictException : InvalidOperationException
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}