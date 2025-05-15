using System;

namespace BUILD.ING.Models
{
    public class DocumentPermission
    {
        public int DocumentId { get; set; }
        public Document Document { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public string PermissionType { get; set; } = "read";
        public DateTime GrantedAt { get; set; }
        public int? GrantedBy { get; set; }
        public User GrantedByUser { get; set; }
    }
}
