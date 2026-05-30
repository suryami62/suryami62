namespace suryami62.Domain.Models;

public interface IConcurrencyTrackedEntity
{
    uint Version { get; set; }
}