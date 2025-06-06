using NpgsqlTypes;
namespace BUILD.ING.Dto
{
    public class BuildingCreateDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int? ConstructionYear { get; set; }
        public decimal? TotalArea { get; set; }
        public int? Floors { get; set; }
        public string Description { get; set; }
        public int OrganizationId { get; set; }
        public NpgsqlPoint? Coordinates { get; set; }
    }
}
