using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BUILD.ING.Models;       
using BUILD.ING.Data;

namespace BUILD.ING.Data.Seed
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync(); // Ensures the DB is up to date

            if (!context.Organizations.Any())
            {
                var org1 = new Organization
                {
                    Name = "Organization Alpha",
                    Users = new List<User>
                    {
                        new User { Username = "alpha_user1", Email = "alpha1@example.com", PasswordHash = "dummyhash123",
                        FirstName = "Alice", LastName = "Anderson"  },
                        new User { Username = "alpha_user2", Email = "alpha2@example.com", PasswordHash = "dummyhash456",
                        FirstName = "Alice", LastName = "Anderson" }
                    }
                };

                var org2 = new Organization
                {
                    Name = "Organization Beta",
                    Users = new List<User>
                    {
                        new User { Username = "beta_user1", Email = "beta1@example.com", PasswordHash = "dummyhash234", 
                        FirstName = "Alice", LastName = "Anderson" },
                        new User { Username = "beta_user2", Email = "beta2@example.com", PasswordHash = "dummyhash345",
                        FirstName = "Alice", LastName = "Anderson" }
                    }
                };

                context.Organizations.AddRange(org1, org2);
                await context.SaveChangesAsync();
            }
        }
    }
}
