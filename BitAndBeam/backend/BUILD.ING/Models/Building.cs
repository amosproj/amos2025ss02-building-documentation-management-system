using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace BUILD.ING.Models
{
    public class Building
    {
        public int BuildingId { get; set; }
        public string Name { get; set; }
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string? Country { get; set; }
        public int? ConstructionYear { get; set; }
        public decimal? TotalArea { get; set; }
        public int? Floors { get; set; }
        public string Description { get; set; }
        public NpgsqlPoint? Coordinates { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }

        [JsonIgnore]
        public ICollection<Document> Documents { get; set; }
        public ICollection<BuildingDocumentRelation> BuildingDocumentRelations { get; set; }
    }
}
