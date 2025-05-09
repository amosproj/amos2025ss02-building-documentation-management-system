using Microsoft.EntityFrameworkCore;
using BuildIngBackend.Models;

namespace Building.Data
{
    public class BuildingDbContext : DbContext
    {
        public BuildingDbContext(DbContextOptions<BuildingDbContext> options)
            : base(options) { }

        // DbSets represent tables
        public DbSet<User> Users { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        // ... (other DbSets)

        // This method is used to configure the model using Fluent API
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Example: Configure self-referencing category relationship
            modelBuilder.Entity<DocumentCategory>()
                .HasOne(dc => dc.ParentCategory)
                .WithMany(dc => dc.SubCategories)
                .HasForeignKey(dc => dc.ParentCategoryId)
                .OnDelete(DeleteBehavior.SetNull);  // Matches SQL ON DELETE SET NULL

            // You can add more configurations here for other entities
        }
    }
}
