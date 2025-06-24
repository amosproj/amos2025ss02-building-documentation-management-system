namespace BUILD.ING.Dto
{
    public class DocumentDto
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
        public string Version { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string Metadata { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedAt { get; set; }
        public string GroupId { get; set; }
    }
}
