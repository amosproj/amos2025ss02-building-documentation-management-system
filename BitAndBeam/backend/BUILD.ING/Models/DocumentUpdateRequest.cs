namespace BUILD.ING.Models
{
    /// <summary>
    /// Request model for updating document metadata
    /// </summary>
    public class DocumentUpdateRequest
    {
        /// <summary>
        /// New title, category and/or building of the document. If a property is null, the original value is kept.
        /// </summary>
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Building { get; set; }
    }
}
