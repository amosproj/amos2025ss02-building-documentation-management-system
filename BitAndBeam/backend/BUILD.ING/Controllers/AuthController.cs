using Microsoft.AspNetCore.Mvc;
using BUILD.ING.Models;
using BUILD.ING.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Authenticates a user with email and password.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Find user by email
            var user = await _db.Users.Include(u => u.Organization)
                                      .FirstOrDefaultAsync(u => u.Email == request.Email);

            // Return 401 if no match or password incorrect
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Just a temporary response for now (JWT will come later)
            return Ok(new { message = "Login successful!" });
        }
    }
}
