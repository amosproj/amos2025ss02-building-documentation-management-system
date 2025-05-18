using BUILD.ING.Models;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace BUILD.ING.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentTag> DocumentTags { get; set; }
        public DbSet<DocumentTagRelation> DocumentTagRelations { get; set; }
        public DbSet<DocumentPermission> DocumentPermissions { get; set; }
        public DbSet<BuildingDocumentRelation> BuildingDocumentRelations { get; set; }
        public DbSet<Organization> Organizations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Organization
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(o => o.OrganizationId);
                entity.HasIndex(o => o.Name).IsUnique();
                entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
                entity.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            // DocumentCategory self-reference
            modelBuilder.Entity<DocumentCategory>(entity =>
            {
                entity.HasKey(dc => dc.CategoryId);

                entity.HasOne(dc => dc.ParentCategory)
                      .WithMany(dc => dc.SubCategories)
                      .HasForeignKey(dc => dc.ParentCategoryId);

            });

            // DocumentTagRelation (many-to-many)
            modelBuilder.Entity<DocumentTagRelation>()
                .HasKey(dtr => new { dtr.DocumentId, dtr.TagId });

            modelBuilder.Entity<DocumentTagRelation>()
                .HasOne(dtr => dtr.Document)
                .WithMany(d => d.DocumentTagRelations)
                .HasForeignKey(dtr => dtr.DocumentId);
            modelBuilder.Entity<DocumentTagRelation>()
                .HasOne(dtr => dtr.Tag)
                .WithMany(t => t.DocumentTagRelations)
                .HasForeignKey(dtr => dtr.TagId);

            // DocumentPermission (many-to-many)
            modelBuilder.Entity<DocumentPermission>()
                .HasKey(dp => new { dp.DocumentId, dp.UserId });
            modelBuilder.Entity<DocumentPermission>()
                .HasOne(dp => dp.Document)
                .WithMany(d => d.DocumentPermissions)
                .HasForeignKey(dp => dp.DocumentId);
            modelBuilder.Entity<DocumentPermission>()
                .HasOne(dp => dp.User)
                .WithMany(u => u.DocumentPermissions)
                .HasForeignKey(dp => dp.UserId);
            modelBuilder.Entity<DocumentPermission>()
                .HasOne(dp => dp.GrantedByUser)
                .WithMany()
                .HasForeignKey(dp => dp.GrantedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // BuildingDocumentRelation (many-to-many)
            modelBuilder.Entity<BuildingDocumentRelation>()
                .HasKey(bdr => new { bdr.BuildingId, bdr.DocumentId });
            modelBuilder.Entity<BuildingDocumentRelation>()
                .HasOne(bdr => bdr.Building)
                .WithMany(b => b.BuildingDocumentRelations)
                .HasForeignKey(bdr => bdr.BuildingId);
            modelBuilder.Entity<BuildingDocumentRelation>()
                .HasOne(bdr => bdr.Document)
                .WithMany(d => d.BuildingDocumentRelations)
                .HasForeignKey(bdr => bdr.DocumentId);

            // Document - Building (optional)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Building)
                .WithMany(b => b.Documents)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Document - Uploader (optional)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Uploader)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Document - Category (optional)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Building>().ToTable("Buildings");
        }
    }
}
