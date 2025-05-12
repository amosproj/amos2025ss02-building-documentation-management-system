using Microsoft.EntityFrameworkCore;
using Build.ING.Models;

namespace Build.ING.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
}
