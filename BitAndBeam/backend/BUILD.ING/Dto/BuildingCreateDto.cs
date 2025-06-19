using NpgsqlTypes;
namespace BUILD.ING.Dto
{
    public class BuildingCreateDto
    {
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
        public int OrganizationId { get; set; }
        public NpgsqlPoint? Coordinates { get; set; }
    }
}
