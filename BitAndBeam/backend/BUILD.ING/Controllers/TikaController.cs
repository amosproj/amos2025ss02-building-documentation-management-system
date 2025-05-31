using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BitAndBeam.Tika
{
    /// <summary>
    /// Controller for Apache Tika document extraction functionality
    /// </summary>
    [ApiController]
    [Route("api/tika")]
    public class TikaController : ControllerBase
    {
        private readonly TikaService _tikaService;
        private readonly ILogger<TikaController> _logger;

        public TikaController(TikaService tikaService, ILogger<TikaController> logger)
        {
            _tikaService = tikaService;
            _logger = logger;
        }

        /// <summary>
        /// Extracts text content from a document
        /// </summary>
        /// <param name="file">The document file to extract text from</param>
        /// <returns>Extracted text content or error information</returns>
        // POST: api/tika/extract
        [HttpPost("extract")]
        public async Task<IActionResult> Extract([FromForm] Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new {
                    success = false,
                    error = new {
                        code = "NO_FILE",
                        message = "No file was uploaded."
                    }
                });
            }
            try
            {
                byte[] fileBytes;
                using (var ms = new System.IO.MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }
                var textResult = await _tikaService.ExtractTextAsync(fileBytes, file.FileName);

                // Detect known error messages from TikaService
                if (textResult == "Could not extract text from the document.")
                {
                    return StatusCode(500, new {
                        success = false,
                        error = new {
                            code = "EXTRACTION_FAILED",
                            message = "Failed to extract text from the provided document."
                        }
                    });
                }
                if (textResult == "Document extraction service is currently unavailable.")
                {
                    return StatusCode(503, new {
                        success = false,
                        error = new {
                            code = "SERVICE_UNAVAILABLE",
                            message = textResult
                        }
                    });
                }
                if (textResult == "Document extraction timed out. Please try again.")
                {
                    return StatusCode(504, new {
                        success = false,
                        error = new {
                            code = "TIMEOUT",
                            message = textResult
                        }
                    });
                }
                if (textResult == "An unexpected error occurred during document extraction.")
                {
                    return StatusCode(500, new {
                        success = false,
                        error = new {
                            code = "UNEXPECTED_ERROR",
                            message = textResult
                        }
                    });
                }
                // Success
                return Ok(new {
                    success = true,
                    content = textResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in TikaController.Extract");
                return StatusCode(500, new {
                    success = false,
                    error = new {
                        code = "UNHANDLED_EXCEPTION",
                        message = "An unhandled error occurred during extraction.",
                        details = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Extracts metadata from a document
        /// </summary>
        /// <param name="file">The document file to extract metadata from</param>
        /// <returns>JSON metadata or error information</returns>
        // POST: api/tika/metadata
        [HttpPost("metadata")]
        public async Task<IActionResult> ExtractMetadata([FromForm] Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new {
                    success = false,
                    error = new {
                        code = "NO_FILE",
                        message = "No file was uploaded."
                    }
                });
            }
            
            try
            {
                byte[] fileBytes;
                using (var ms = new System.IO.MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }
                
                var metadataResult = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName);
                
                // Check for known error messages
                if (metadataResult.Contains("Could not extract metadata") || 
                    metadataResult.Contains("Document extraction service is currently unavailable") ||
                    metadataResult.Contains("Document metadata extraction timed out") ||
                    metadataResult.Contains("An unexpected error occurred"))
                {
                    return StatusCode(500, new {
                        success = false,
                        error = new {
                            code = "METADATA_EXTRACTION_FAILED",
                            message = metadataResult
                        }
                    });
                }
                
                // Success
                return Ok(new {
                    success = true,
                    metadata = metadataResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in TikaController.ExtractMetadata");
                return StatusCode(500, new {
                    success = false,
                    error = new {
                        code = "UNHANDLED_EXCEPTION",
                        message = "An unhandled error occurred during metadata extraction.",
                        details = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Extracts both text content and metadata from a document in a single call
        /// </summary>
        /// <param name="file">The document file to process</param>
        /// <returns>JSON object containing both extracted text and structured metadata</returns>
        // POST: api/tika/process
        [HttpPost("process")]
        public async Task<IActionResult> ProcessDocument([FromForm] Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new {
                    success = false,
                    error = new {
                        code = "NO_FILE",
                        message = "No file was uploaded."
                    }
                });
            }
            
            try
            {
                byte[] fileBytes;
                using (var ms = new System.IO.MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }
                
                // Extract text
                var textResult = await _tikaService.ExtractTextAsync(fileBytes, file.FileName);
                
                // Extract metadata
                var metadataResult = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName);
                
                // Check for error conditions
                bool textSuccess = !textResult.Contains("Could not extract text") && 
                                   !textResult.Contains("Document extraction service is currently unavailable") && 
                                   !textResult.Contains("Document extraction timed out") && 
                                   !textResult.Contains("An unexpected error occurred");
                
                bool metadataSuccess = !metadataResult.Contains("Could not extract metadata") && 
                                      !metadataResult.Contains("Document extraction service is currently unavailable") && 
                                      !metadataResult.Contains("Document metadata extraction timed out") && 
                                      !metadataResult.Contains("An unexpected error occurred");
                
                if (!textSuccess && !metadataSuccess)
                {
                    return StatusCode(500, new {
                        success = false,
                        error = new {
                            code = "PROCESSING_FAILED",
                            message = "Failed to extract both text and metadata from the document."
                        }
                    });
                }
                
                // Return combined result with appropriate success flags
                return Ok(new {
                    success = true,
                    data = new {
                        text = new {
                            success = textSuccess,
                            content = textSuccess ? textResult : null,
                            error = !textSuccess ? textResult : null
                        },
                        metadata = new {
                            success = metadataSuccess,
                            content = metadataSuccess ? metadataResult : null,
                            error = !metadataSuccess ? metadataResult : null
                        },
                        file_info = new {
                            name = file.FileName,
                            size = file.Length,
                            content_type = file.ContentType
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in TikaController.ProcessDocument");
                return StatusCode(500, new {
                    success = false,
                    error = new {
                        code = "UNHANDLED_EXCEPTION",
                        message = "An unhandled error occurred during document processing.",
                        details = ex.Message
                    }
                });
            }
        }
    }
}