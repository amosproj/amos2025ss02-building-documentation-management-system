using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Building.Models
{
    // Maps this class to the "document_categories" table in PostgreSQL
    [Table("document_categories")]
    public class DocumentCategory
    {
        // Maps to "category_id" column and is the primary key (auto-incremented)
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        // Maps to "name", required, max 50 characters
        [Required]
        [MaxLength(50)]
        [Column("name")]
        public string Name { get; set; }

        // Maps to "description", optional text field
        [Column("description")]
        public string? Description { get; set; }

        // Maps to "parent_category_id", which references another category
        [Column("parent_category_id")]
        public int? ParentCategoryId { get; set; }

        // Navigation property for parent category (self-referencing relationship)
        [ForeignKey("ParentCategoryId")]
        public DocumentCategory? ParentCategory { get; set; }

        // Navigation property for child categories (inverse of the self-reference)
        public ICollection<DocumentCategory>? SubCategories { get; set; }

        // Maps to "created_at", with a default value handled by the DB
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
