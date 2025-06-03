using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BUILD.ING.Models
{
    /// <summary>
    /// Model for file upload operations
    /// </summary>
    public class FileUploadModel
    {
        /// <summary>
        /// The file to be uploaded
        /// </summary>
        [Required]
        public IFormFile File { get; set; }
    }
}

