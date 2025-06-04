using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BUILD.ING.Data;
using BUILD.ING.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; //

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BuildingsController> _logger;  // ADDED: Inject ILogger for logging
        public BuildingsController(AppDbContext context, ILogger<BuildingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/Buildings
        // Creates a new building and returns its ID
        [HttpPost]
        public async Task<IActionResult> CreateBuilding(Building building)
        {
            _logger.LogInformation("CreateBuilding called at {Time}", DateTime.UtcNow); // ADDED: Log method entry

            building.CreatedAt = DateTime.UtcNow;
            building.UpdatedAt = DateTime.UtcNow;

            _context.Buildings.Add(building);

            try
            {
                await _context.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Building created successfully with ID {BuildingId}", building.BuildingId); // ADDED: Log success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating building"); // ADDED: Log exception
                return StatusCode(500, ex.Message);
            }

            return Ok(new { id = building.BuildingId });
        }

        // GET: api/Buildings
        // Returns a list of all buildings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Building>>> GetBuildings()
        {
            _logger.LogInformation("GetBuildings called at {Time}", DateTime.UtcNow);

            var buildings = await _context.Buildings.ToListAsync().ConfigureAwait(false);

            _logger.LogInformation("GetBuildings returned {Count} records", buildings.Count);

            return buildings;
        }

        // GET: api/Buildings/{id}
        // Returns a single building by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Building>> GetBuilding(int id)
        {
            _logger.LogInformation("GetBuilding called for ID {BuildingId} at {Time}", id, DateTime.UtcNow);

            var building = await _context.Buildings
                .Include(b => b.Documents)
                .Include(b => b.BuildingDocumentRelations)
                .FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);

            if (building == null)
            {
                _logger.LogWarning("GetBuilding did not find building with ID {BuildingId}", id);
                return NotFound();
            }

            _logger.LogInformation("GetBuilding found building with ID {BuildingId}", id);

            return building;
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

            _context.Entry(existingBuilding).CurrentValues.SetValues(updatedBuilding);


            // Update only editable fields
            existingBuilding.Name = updatedBuilding.Name;
            existingBuilding.Address = updatedBuilding.Address;
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
            _logger.LogInformation("DeleteBuilding called for ID {BuildingId} at {Time}", id, DateTime.UtcNow);

            var building = await _context.Buildings
                .Include(b => b.BuildingDocumentRelations)
                .FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);

            if (building == null)
            {
                _logger.LogWarning("DeleteBuilding could not find building with ID {BuildingId}", id);
                return NotFound();
            }

            try
            {
                _context.BuildingDocumentRelations.RemoveRange(building.BuildingDocumentRelations);
                _context.Buildings.Remove(building);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("DeleteBuilding successfully deleted building with ID {BuildingId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting building with ID {BuildingId}", id);
                return StatusCode(500, ex.Message);
            }

            return NoContent();
        }
    }
}
