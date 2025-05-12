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

            // Configure the "Document" entity's relationships using Fluent API
            modelBuilder.Entity<Document>()
            // Define relationship between Document and DocumentCategory:
            // A document optionally belongs to one category (nullable FK),
            // and a category can be associated with multiple documents.
                .HasOne(d => d.Category)            // Navigation property to the parent category
                .WithMany()                         // No reverse navigation property defined in category
                .HasForeignKey(d => d.CategoryId)   // Link by the foreign key 'category_id'
                .OnDelete(DeleteBehavior.SetNull);  // When the category is deleted, set FK to null

            // Define relationship between Document and Building:
            // A document must be linked to a building (non-nullable FK),
            // and when the building is deleted, all its documents are deleted too.

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Building)
                .WithMany()
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Define relationship between Document and User (who uploaded it):
            // A document may have been uploaded by a user (optional),
            // and if the user is deleted, the 'uploaded_by' field is set to null.

            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.SetNull);


            
        }
    }
}
