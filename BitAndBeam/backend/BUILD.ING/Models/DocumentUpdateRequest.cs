namespace Build.ING.Models
{
    /// <summary>
    /// Request model for updating document metadata
    /// </summary>
    public class DocumentUpdateRequest
    {
        /// <summary>
        /// New title of the document
        /// </summary>
        public string Title { get; set; } = string.Empty;
    }
}
