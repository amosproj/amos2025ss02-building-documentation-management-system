using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BUILD.ING.Data;
using BUILD.ING.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace BUILD.ING.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BuildingsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserGroupId()
        {
            /*var groupClaim = User.Claims.FirstOrDefault(c =>
                c.Type == "groupId" ||
                c.Type.EndsWith("groupid", StringComparison.OrdinalIgnoreCase));

            if (groupClaim == null)
            {
                Console.WriteLine("⚠ Available claims:");
                foreach (var claim in User.Claims)
                    Console.WriteLine($"- {claim.Type}: {claim.Value}");

                throw new UnauthorizedAccessException("No group ID in token.");
            }

            return int.Parse(groupClaim.Value);*/
            return -1;
        }

        /*[HttpGet("generate-token")]
        public IActionResult GenerateToken() =>
        Ok(JwtGenerator.GenerateToken("testuser", 42, "test", 120));*/


        // POST: api/Buildings
        // Creates a new building and returns its ID
        [HttpPost]
        public async Task<IActionResult> CreateBuilding(Building building)
        {
            int groupId = GetUserGroupId();

             // Assign group ID to the new building
            building.GroupId = groupId;

            building.CreatedAt = DateTime.UtcNow;
            building.UpdatedAt = DateTime.UtcNow;

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();

            return Ok(new { id = building.BuildingId });
        }

       /* [HttpGet("me")]
        public IActionResult WhoAmI()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized("Token was not accepted.");
            }

            return Ok(new
            {
                user = User.Identity.Name,
                claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpGet("debug-token")]
        public IActionResult DebugToken()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }*/



        // GET: api/Buildings
        // Returns a list of all buildings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Building>>> GetBuildings()
        {
            int groupId = GetUserGroupId();

            return await _context.Buildings
                //.Where(b => b.GroupId == groupId)
                .ToListAsync();
        }

        // GET: api/Buildings/{id}
        // Returns a single building by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Building>> GetBuilding(int id)
        {
            int groupId = GetUserGroupId();

            var building = await _context.Buildings
               // .Where(b.BuildingId == id) //b => b.GroupId == groupId && 
                .Include(b => b.Documents)
                .Include(b => b.BuildingDocumentRelations)
                .FirstOrDefaultAsync();

            if (building == null)
                return NotFound();

            return building;
        }

        // PUT: api/Buildings/{id}
        // Updates a building by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBuilding(int id, [FromBody] Building updatedBuilding)
        {
            int groupId = GetUserGroupId();
            
            if (id != updatedBuilding.BuildingId)
                return BadRequest("Mismatched Building ID");

            var existingBuilding = await _context.Buildings
                .FirstOrDefaultAsync(b => b.BuildingId == id); // && b.GroupId == groupId

            if (existingBuilding == null)
                return NotFound();

            // Update only editable fields
            existingBuilding.Name = updatedBuilding.Name;
            existingBuilding.Address = updatedBuilding.Address;
            existingBuilding.ConstructionYear = updatedBuilding.ConstructionYear;
            existingBuilding.TotalArea = updatedBuilding.TotalArea;
            existingBuilding.Floors = updatedBuilding.Floors;
            existingBuilding.Description = updatedBuilding.Description;
            existingBuilding.Coordinates = updatedBuilding.Coordinates;
            existingBuilding.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Buildings/{id}
        // Deletes a building and its related documents
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBuilding(int id)
        {
            int groupId = GetUserGroupId();

            var building = await _context.Buildings
                .Include(b => b.BuildingDocumentRelations)
                .FirstOrDefaultAsync(b => b.BuildingId == id); // && b.GroupId == groupId

            if (building == null)
                return NotFound();

            // Remove related building-document relations
            _context.BuildingDocumentRelations.RemoveRange(building.BuildingDocumentRelations);

            // Optionally remove documents if they are not shared
            // _context.Documents.RemoveRange(building.Documents);

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
