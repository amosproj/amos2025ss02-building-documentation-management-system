using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BUILD.ING.Data;
using BUILD.ING.Dto;
using BUILD.ING.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BuildingsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Buildings
        // Creates a new building and returns its ID
        [HttpPost]
        public async Task<IActionResult> CreateBuilding([FromBody] BuildingCreateDto dto)
        {
            // ✅ Convert coordinate data if present
            NpgsqlPoint? coordinates = null;
            if (dto.Coordinates.HasValue)
            {
                coordinates = new NpgsqlPoint(dto.Coordinates.Value.X, dto.Coordinates.Value.Y);
            }


            var building = new Building
            {
                Name = dto.Name,
                StreetName = dto.StreetName,
                HouseNumber = dto.HouseNumber,
                PostalCode = dto.PostalCode,
                City = dto.City,
                Country = dto.Country,
                ConstructionYear = dto.ConstructionYear,
                TotalArea = dto.TotalArea,
                Floors = dto.Floors,
                Description = dto.Description,
                OrganizationId = dto.OrganizationId,
                Coordinates = coordinates,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                BuildingDocumentRelations = new List<BuildingDocumentRelation>(),
                Documents = new List<Document>()
            };

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return Ok(new { id = building.BuildingId });
        }

        // GET: api/Buildings
        // Returns a list of all buildings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BuildingDto>>> GetBuildings()
        {
            var buildings = await _context.Buildings.ToListAsync().ConfigureAwait(false);
            var buildingIds = buildings.Select(b => b.BuildingId).ToList();
            var documents = _context.Documents
                .Where(d => d.BuildingId.HasValue && buildingIds.Contains(d.BuildingId.Value))
                .Select(d => new { d.BuildingId, d.DocumentId, d.Title })
                .ToList();
            var orgMap = _context.Organizations.ToDictionary(o => o.OrganizationId, o => o.Name);
            var dtos = buildings.Select(b => new BuildingDto
            {
                BuildingId = b.BuildingId,
                Name = b.Name,
                StreetName = b.StreetName,
                HouseNumber = b.HouseNumber,
                PostalCode = b.PostalCode,
                City = b.City,
                Country = b.Country,
                ConstructionYear = b.ConstructionYear,
                TotalArea = b.TotalArea,
                Floors = b.Floors,
                Description = b.Description,
                Coordinates = b.Coordinates?.ToString(),
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                OrganizationId = b.OrganizationId,
                OrganizationName = orgMap.ContainsKey(b.OrganizationId) ? orgMap[b.OrganizationId] : null,
                Documents = documents.Where(d => d.BuildingId == b.BuildingId)
                    .Select(d => new KeyValuePair<int, string>(d.DocumentId, d.Title)).ToList()
            }).ToList();
            return Ok(dtos);
        }

        // GET: api/Buildings/{id}
        // Returns a single building by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<BuildingDto>> GetBuilding(int id)
        {
            var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);
            if (building == null)
                return NotFound();
            var orgName = _context.Organizations.Where(o => o.OrganizationId == building.OrganizationId).Select(o => o.Name).FirstOrDefault();
            var documents = _context.Documents
                .Where(d => d.BuildingId == id)
                .Select(d => new KeyValuePair<int, string>(d.DocumentId, d.Title))
                .ToList();
            var dto = new BuildingDto
            {
                BuildingId = building.BuildingId,
                Name = building.Name,
                StreetName = building.StreetName,
                HouseNumber = building.HouseNumber,
                PostalCode = building.PostalCode,
                City = building.City,
                Country = building.Country,
                ConstructionYear = building.ConstructionYear,
                TotalArea = building.TotalArea,
                Floors = building.Floors,
                Description = building.Description,
                Coordinates = building.Coordinates?.ToString(),
                CreatedAt = building.CreatedAt,
                UpdatedAt = building.UpdatedAt,
                OrganizationId = building.OrganizationId,
                OrganizationName = orgName,
                Documents = documents
            };
            return Ok(dto);
        }

        // PUT: api/Buildings/{id}
        // Updates a building by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBuilding(int id, [FromBody] Building updatedBuilding)
        {
            if (id != updatedBuilding.BuildingId)
                return BadRequest("Mismatched Building ID");

            //var existingBuilding = await _context.Buildings.FindAsync(id).ConfigureAwait(false);
            var existingBuilding = await _context.Buildings
                .Include(b => b.Documents)
                .Include(b => b.BuildingDocumentRelations)
                .FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);


            if (existingBuilding == null)
                return NotFound();

            // If the incoming organizationId is null or 0, preserve existing one
            if (updatedBuilding.OrganizationId == 0)
                updatedBuilding.OrganizationId = existingBuilding.OrganizationId;

            _context.Entry(existingBuilding).CurrentValues.SetValues(updatedBuilding);


            // Update only editable fields
            existingBuilding.Name = updatedBuilding.Name;
            existingBuilding.StreetName = updatedBuilding.StreetName;
            existingBuilding.HouseNumber = updatedBuilding.HouseNumber;
            existingBuilding.PostalCode = updatedBuilding.PostalCode;
            existingBuilding.City = updatedBuilding.City;
            existingBuilding.Country = updatedBuilding.Country;
            existingBuilding.ConstructionYear = updatedBuilding.ConstructionYear;
            existingBuilding.TotalArea = updatedBuilding.TotalArea;
            existingBuilding.Floors = updatedBuilding.Floors;
            existingBuilding.Description = updatedBuilding.Description;
            existingBuilding.Coordinates = updatedBuilding.Coordinates;
            existingBuilding.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingBuilding).Property(b => b.UpdatedAt).IsModified = true;
            _context.Entry(existingBuilding).Property(b => b.Coordinates).IsModified = true;

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("‼️ ERROR during SaveChanges: " + ex.Message);
                return StatusCode(500, ex.Message);
            }


            //await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("debug-db")]
        public IActionResult GetDbInfo()
        {
            var conn = _context.Database.GetDbConnection();
            return Ok(new
            {
                conn.Database,
                conn.DataSource,
                conn.ConnectionString
            });
        }

        [HttpGet("debug-full")]
        public IActionResult GetDebugFull()
        {
            var conn = _context.Database.GetDbConnection();
            return Ok(new
            {
                Provider = _context.Database.ProviderName,
                Connection = conn.ConnectionString,
                Source = conn.DataSource,
                Db = conn.Database
            });
        }



        // DELETE: api/Buildings/{id}
        // Deletes a building and its related documents
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBuilding(int id)
        {
            var building = await _context.Buildings
                .Include(b => b.BuildingDocumentRelations)
                .FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);

            if (building == null)
                return NotFound();

            // Remove related building-document relations
            _context.BuildingDocumentRelations.RemoveRange(building.BuildingDocumentRelations);

            // Optionally remove documents if they are not shared
            // _context.Documents.RemoveRange(building.Documents);

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return NoContent();
        }
    }
}
