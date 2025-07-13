using BitAndBeam.Models;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace BitAndBeam.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Building> Buildings { get; set; }

        public DbSet<Document> Documents { get; set; }

        public DbSet<BuildingDocumentRelation> BuildingDocumentRelations { get; set; }
        public DbSet<Organization> Organizations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

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

            // Document - Building: When a building is deleted, delete all its documents
            modelBuilder.Entity<Document>()
                .HasOne<Building>()
                .WithMany()
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Document - Uploader (optional)
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Uploader)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.SetNull);



            modelBuilder.Entity<Document>()
                .Property(d => d.CategoryName);


            // Configure JSONB columns for Document
            modelBuilder.Entity<Document>()
                .Property(d => d.KeyInformation)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Building>().ToTable("Buildings");
        }
    }
}


