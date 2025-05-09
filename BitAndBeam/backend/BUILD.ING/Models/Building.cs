using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries; // Needed to map PostgreSQL POINT to EF Core (via Npgsql types)

namespace Building.Models
{
    // Maps this class to the "buildings" table in PostgreSQL
    [Table("buildings")]
    public class Building
    {
        // Maps to "building_id", which is the primary key
        [Key]
        [Column("building_id")]
        public int BuildingId { get; set; }

        // Maps to "name", required and max 100 characters
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; }

        // Maps to "address", optional text
        [Column("address")]
        public string? Address { get; set; }

        // Maps to "construction_year", optional integer
        [Column("construction_year")]
        public int? ConstructionYear { get; set; }

        // Maps to "total_area", optional decimal with 2 decimal places
        [Column("total_area", TypeName = "decimal(10,2)")]
        public decimal? TotalArea { get; set; }

        // Maps to "floors", optional integer
        [Column("floors")]
        public int? Floors { get; set; }

        // Maps to "description", optional text
        [Column("description")]
        public string? Description { get; set; }

        // Maps to "coordinates" as a POINT — must use NetTopologySuite
        [Column("coordinates")]
        public Point? Coordinates { get; set; }

        // Maps to "created_at", default value in DB
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Maps to "updated_at", default value in DB
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
