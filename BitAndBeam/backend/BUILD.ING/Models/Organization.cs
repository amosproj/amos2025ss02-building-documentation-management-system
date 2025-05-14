using System;
using System.Collections.Generic;

namespace BUILD.ING.Models
{
    public class Organization
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties (optional, for future relationships)
        public ICollection<User>? Users { get; set; }
        public ICollection<Building>? Buildings { get; set; }
    }
}
