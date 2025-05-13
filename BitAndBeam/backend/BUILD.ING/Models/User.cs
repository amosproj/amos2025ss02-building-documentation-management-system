using System;
using System.Collections.Generic;

namespace BUILD.ING.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } = "viewer";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Document> UploadedDocuments { get; set; }
        public ICollection<DocumentPermission> DocumentPermissions { get; set; }
    }
}