using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
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

            // 🔐 Generate JWT token
            var jwtSecret = _config["JwtSecret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Set the claims to include in the token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("orgId", user.OrganizationId.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Build the token
            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(1), // 1 hour validity
                signingCredentials: creds,
                claims: claims
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Return the token and user info to the frontend
            return Ok(new
            {
                token = tokenString,
                expiresIn = 3600, // in seconds
                user = new
                {
                    id = user.UserId,
                    email = user.Email,
                    organizationId = user.OrganizationId,
                    role = user.Role
                }
            });
        }
    }
}
