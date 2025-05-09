using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;

namespace Building.Models
{
    // Maps this class to the "documents" table in the PostgreSQL database
    [Table("documents")]
    public class Document
    {
        // Primary key: auto-incremented document ID
        [Key]
        [Column("document_id")]
        public int DocumentId { get; set; }

        // Title of the document (required, max length 255)
        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string Title { get; set; }

        // File system path to the stored document (required)
        [Required]
        [MaxLength(255)]
        [Column("file_path")]
        public string FilePath { get; set; }

        // MIME or file extension type (e.g., PDF, JPG) (required)
        [Required]
        [MaxLength(20)]
        [Column("file_type")]
        public string FileType { get; set; }

        // Size of the file in bytes (required)
        [Required]
        [Column("file_size")]
        public int FileSize { get; set; }

        // Foreign key to document category (nullable)
        [Column("category_id")]
        public int? CategoryId { get; set; }

        // Navigation property to DocumentCategory
        public DocumentCategory? Category { get; set; }

        // Foreign key to associated building (required by DB, ON DELETE CASCADE)
        [Column("building_id")]
        public int BuildingId { get; set; }

        // Navigation property to Building
        public Building Building { get; set; }

        // Foreign key to user who uploaded the document (nullable)
        [Column("uploaded_by")]
        public int? UploadedById { get; set; }

        // Navigation property to User
        public User? UploadedBy { get; set; }

        // Upload timestamp, set automatically by DB
        [Column("upload_date")]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // Last modified timestamp, also set by DB
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Optional version string (default '1.0')
        [MaxLength(20)]
        [Column("version")]
        public string Version { get; set; } = "1.0";

        // Status with CHECK constraint in DB (draft/review/approved/archived)
        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "draft";

        // Optional description of the document
        [Column("description")]
        public string? Description { get; set; }

        // Indicates whether the document is publicly visible
        [Column("is_public")]
        public bool IsPublic { get; set; } = false;

        // Flexible metadata stored as JSONB in PostgreSQL
        [Column("metadata", TypeName = "jsonb")]
        public JsonObject? Metadata { get; set; }
    }
}
