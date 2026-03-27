#region

using Microsoft.EntityFrameworkCore;
using suryami62.Data;

#endregion

namespace suryami62.Infrastructure.Tests;

internal static class DbContextFactory
{
    /// <summary>
    ///     Creates an <see cref="ApplicationDbContext" /> backed by an EF Core in-memory database.
    ///     Each call using a unique name creates an isolated database instance.
    /// </summary>
    internal static ApplicationDbContext CreateInMemory(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}