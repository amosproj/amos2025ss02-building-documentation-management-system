using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BUILD.ING.Data;
using BUILD.ING.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingsController : ControllerBase
    {
        private readonly AppDbContext _context;
private readonly ILogger<BuildingsController> _logger;

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
    try
    {
        building.CreatedAt = DateTime.UtcNow;
        building.UpdatedAt = DateTime.UtcNow;
        _context.Buildings.Add(building);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { id = building.BuildingId });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating building");
        return StatusCode(500, new {
            success = false,
            error = new {
                code = "CREATE_BUILDING_FAILED",
                message = "Failed to create building.",
                details = ex.Message
            }
        });
    }
}

        // GET: api/Buildings
        // Returns a list of all buildings
        [HttpGet]
public async Task<IActionResult> GetBuildings()
{
    try
    {
        //We can apply later group-based filtering here
        var buildings = await _context.Buildings.ToListAsync().ConfigureAwait(false);
        return Ok(buildings);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching buildings");
        return StatusCode(500, new {
            success = false,
            error = new {
                code = "GET_BUILDINGS_FAILED",
                message = "Failed to fetch buildings.",
                details = ex.Message
            }
        });
    }
}

        // GET: api/Buildings/{id}
        // Returns a single building by ID
        [HttpGet("{id}")]
public async Task<IActionResult> GetBuilding(int id)
{
    try
    {
        var building = await _context.Buildings
            .Include(b => b.Documents)
            .Include(b => b.BuildingDocumentRelations)
            .FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);
        if (building == null)
            return NotFound(new {
                success = false,
                error = new {
                    code = "BUILDING_NOT_FOUND",
                    message = $"Building with ID {id} not found."
                }
            });
        return Ok(building);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error fetching building {BuildingId}", id);
        return StatusCode(500, new {
            success = false,
            error = new {
                code = "GET_BUILDING_FAILED",
                message = "Failed to fetch building.",
                details = ex.Message
            }
        });
    }
}

        // PUT: api/Buildings/{id}
        // Updates a building by ID
        [HttpPut("{id}")]
public async Task<IActionResult> UpdateBuilding(int id, [FromBody] Building updatedBuilding)
{
    try
    {
        if (id != updatedBuilding.BuildingId)
            return BadRequest(new {
                success = false,
                error = new {
                    code = "MISMATCHED_ID",
                    message = "Mismatched Building ID."
                }
            });
        var existingBuilding = await _context.Buildings.FindAsync(id).ConfigureAwait(false);
        if (existingBuilding == null)
            return NotFound(new {
                success = false,
                error = new {
                    code = "BUILDING_NOT_FOUND",
                    message = $"Building with ID {id} not found."
                }
            });
        // Update only editable fields
        existingBuilding.Name = updatedBuilding.Name;
        existingBuilding.Address = updatedBuilding.Address;
        existingBuilding.ConstructionYear = updatedBuilding.ConstructionYear;
        existingBuilding.TotalArea = updatedBuilding.TotalArea;
        existingBuilding.Floors = updatedBuilding.Floors;
        existingBuilding.Description = updatedBuilding.Description;
        existingBuilding.Coordinates = updatedBuilding.Coordinates;
        existingBuilding.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating building {BuildingId}", id);
        return StatusCode(500, new {
            success = false,
            error = new {
                code = "UPDATE_BUILDING_FAILED",
                message = "Failed to update building.",
                details = ex.Message
            }
        });
    }
}

        // DELETE: api/Buildings/{id}
        // Deletes a building and its related documents
        [HttpDelete("{id}")]
public async Task<IActionResult> DeleteBuilding(int id)
{
    try
    {
        var building = await _context.Buildings
            .Include(b => b.BuildingDocumentRelations)
            .FirstOrDefaultAsync(b => b.BuildingId == id).ConfigureAwait(false);
        if (building == null)
            return NotFound(new {
                success = false,
                error = new {
                    code = "BUILDING_NOT_FOUND",
                    message = $"Building with ID {id} not found."
                }
            });
        // Remove related building-document relations
        _context.BuildingDocumentRelations.RemoveRange(building.BuildingDocumentRelations);
        // Optionally remove documents if they are not shared
        // _context.Documents.RemoveRange(building.Documents);
        _context.Buildings.Remove(building);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return NoContent();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting building {BuildingId}", id);
        return StatusCode(500, new {
            success = false,
            error = new {
                code = "DELETE_BUILDING_FAILED",
                message = "Failed to delete building.",
                details = ex.Message
            }
        });
    }
}
    }
}
