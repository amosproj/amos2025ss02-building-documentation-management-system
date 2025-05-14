using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Build.ING.Data;

namespace Build.ING.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=DocumentDb;Username=postgres;Password=1234");

        return new AppDbContext(optionsBuilder.Options);
    }
}
