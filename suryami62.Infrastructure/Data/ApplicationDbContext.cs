#region

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using suryami62.Domain.Models;

#endregion

namespace suryami62.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Setting> Settings { get; set; }
}