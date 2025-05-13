using System;
using System.Collections.Generic;

namespace BUILD.ING.Models
{
    public class Document
    {
        public int DocumentId { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public int FileSize { get; set; }
        public int? CategoryId { get; set; }
        public int? BuildingId { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime LastModified { get; set; }
        public string Version { get; set; } = "1.0";
        public string Status { get; set; } = "draft";
        public string Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public string Metadata { get; set; }

        public Building Building { get; set; }
        public User Uploader { get; set; }
        public DocumentCategory Category { get; set; }
        public ICollection<DocumentTagRelation> DocumentTagRelations { get; set; }
        public ICollection<DocumentPermission> DocumentPermissions { get; set; }
        public ICollection<BuildingDocumentRelation> BuildingDocumentRelations { get; set; }
    }
}