using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BUILD.ING.Services;

namespace BUILD.ING.Controllers
{
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
        /// Extracts text content from a document file using Apache Tika
        /// </summary>
        /// <param name="file">The document file to extract text from</param>
        /// <returns>Extracted text content or error information</returns>
        [HttpPost("extract")]
        public async Task<IActionResult> Extract(IFormFile file)
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
                using (var ms = new MemoryStream())
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
    }
}
