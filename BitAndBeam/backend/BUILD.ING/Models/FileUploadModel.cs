using Microsoft.AspNetCore.Http;

namespace BUILD.ING.Models
{
    /// <summary>
    /// Model for handling file uploads
    /// </summary>
    public class FileUploadModel
    {
        /// <summary>
        /// The file being uploaded
        /// </summary>
        public IFormFile File { get; set; }
    }
}
