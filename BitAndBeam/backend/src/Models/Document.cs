using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BitAndBeam.Models
{
    public class Document
    {
        public int DocumentId { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public int FileSize { get; set; }
        public string? CategoryName { get; set; }
        public int? BuildingId { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime LastModified { get; set; }
        public string Version { get; set; } = "1.0";
        public string Status { get; set; } = "draft";
        public string Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public string Metadata { get; set; }
        public JsonDocument? KeyInformation { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; }
        public int OrganizationId { get; set; }
        public User Uploader { get; set; }
        public ICollection<BuildingDocumentRelation> BuildingDocumentRelations { get; set; }
    }
}


