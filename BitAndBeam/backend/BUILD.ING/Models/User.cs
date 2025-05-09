using System;
using System.ComponentenModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Building.Models
{
    // Maps this class to the "users" table in the PostgreSQL database
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("username")]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [MaxLength(50)]
        [Column("first_name")]
        public string FirstName { get; set; }

        [MaxLength(50)]
        [Column("last_name")]
        public string LastName { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("role")]
        public string Role { get; set; } = "viewer";  // default

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
