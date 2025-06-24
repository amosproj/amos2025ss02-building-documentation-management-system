using System;
using System.Collections.Generic;

namespace BUILD.ING.Dto
{
    public class BuildingDto
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
        public string? Coordinates { get; set; } // You may want to format NpgsqlPoint as string or object
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public List<int> Documents { get; set; } = new List<int>();
    }
}
