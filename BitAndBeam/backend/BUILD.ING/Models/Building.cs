using System;
using System.Collections.Generic;
using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;

namespace BUILD.ING.Models
{
    public class Building
    {
        [Key]
        public int BuildingId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int? ConstructionYear { get; set; }
        public decimal? TotalArea { get; set; }
        public int? Floors { get; set; }
        public string Description { get; set; }
        public NpgsqlPoint? Coordinates { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Document> Documents { get; set; }
        public ICollection<BuildingDocumentRelation> BuildingDocumentRelations { get; set; }

        public int GroupId { get; set; }

        // Constructor of the class 
            public Building()
        {
            Documents = new List<Document>();
            BuildingDocumentRelations = new List<BuildingDocumentRelation>();
        }
    }
}
