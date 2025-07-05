using System;
using System.IO;
using System.Threading.Tasks;
using BitAndBeam.Models;
using BitAndBeam.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BitAndBeam.Controllers
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
        /// Receives a document file and returns both extracted text content and structured metadata in a single response.
        /// </summary>
        /// <param name="model">The model containing the document file to process</param>
        /// <returns>JSON object containing both extracted text and structured metadata</returns>
        // POST: api/tika/process
        [HttpPost("process")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessDocument([FromForm] BitAndBeam.Models.FileUploadModel model)
        {
            if (model == null || model.File == null || model.File.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    error = new
                    {
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
                    await model.File.CopyToAsync(ms).ConfigureAwait(false);
                    fileBytes = ms.ToArray();
                }

                // Performance tracking for OCR process
                var sw = System.Diagnostics.Stopwatch.StartNew();
                
                // Check file type to determine optimal processing strategy
                string fileExtension = Path.GetExtension(model.File.FileName).ToLowerInvariant();
                bool isPdf = fileExtension == ".pdf";
                bool isImage = new[] { ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".gif" }.Contains(fileExtension);
                bool needsOcr = isPdf || isImage;
                
                string textResult = null;
                bool ocrApplied = false;

                if (needsOcr)
                {
                    // For documents likely needing OCR, try optimized processing
                    _logger.LogInformation("Processing document that may need OCR: {FileName}", model.File.FileName);
                    
                    // 1) Fast path: try extraction without forcing OCR for PDFs first
                    if (isPdf)
                    {
                        textResult = await _tikaService.ExtractTextAsync(fileBytes, model.File.FileName, false).ConfigureAwait(false);
                        
                        // 2) If not enough text, do a second pass with OCR enabled
                        if (string.IsNullOrWhiteSpace(textResult) || textResult.Trim().Length < 50)
                        {
                            _logger.LogInformation("Initial extraction produced insufficient text, applying OCR: {FileName}", model.File.FileName);
                            textResult = await _tikaService.ExtractTextAsync(fileBytes, model.File.FileName, true).ConfigureAwait(false);
                            ocrApplied = true; // we explicitly triggered OCR
                        }
                        else if (textResult.Contains("class=\"ocr\"", StringComparison.OrdinalIgnoreCase) ||
                                textResult.Contains("class=\"page\"", StringComparison.OrdinalIgnoreCase))
                        {
                            // Tika already applied OCR during the fast path
                            ocrApplied = true;
                        }
                    }
                    else
                    {
                        // For image files, immediately use OCR
                        textResult = await _tikaService.ExtractTextAsync(fileBytes, model.File.FileName, true).ConfigureAwait(false);
                        ocrApplied = true;
                    }
                }
                else
                {
                    // For documents unlikely to need OCR (regular text files, etc.)
                    _logger.LogInformation("Processing regular document: {FileName}", model.File.FileName);
                    textResult = await _tikaService.ExtractTextAsync(fileBytes, model.File.FileName, false).ConfigureAwait(false);
                }
                
                sw.Stop();
                _logger.LogInformation(
                    "Document processing completed in {ElapsedMs}ms for {FileName}, Size: {Size}KB, OCR applied: {OcrApplied}", 
                    sw.ElapsedMilliseconds, model.File.FileName, fileBytes.Length/1024, ocrApplied);


                // Extract metadata
                var metadataResult = await _tikaService.ExtractMetadataAsync(fileBytes, model.File.FileName).ConfigureAwait(false);

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
                    return StatusCode(500, new
                    {
                        success = false,
                        error = new
                        {
                            code = "PROCESSING_FAILED",
                            message = "Failed to extract both text and metadata from the document."
                        }
                    });
                }

                // Return combined result with appropriate success flags
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        text = new
                        {
                            success = textSuccess,
                            ocrApplied,
                            content = textSuccess ? textResult : null,
                            error = !textSuccess ? textResult : null
                        },
                        metadata = new
                        {
                            success = metadataSuccess,
                            content = metadataSuccess ? metadataResult : null,
                            error = !metadataSuccess ? metadataResult : null
                        },
                        file_info = new
                        {
                            name = model.File.FileName,
                            size = model.File.Length,
                            content_type = model.File.ContentType
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in TikaController.ProcessDocument");
                return StatusCode(500, new
                {
                    success = false,
                    error = new
                    {
                        code = "UNHANDLED_EXCEPTION",
                        message = "An unhandled error occurred during document processing.",
                        details = ex.Message
                    }
                });
            }
        }

        /// <summary>
        /// Checks if the Tika server is available and responding
        /// </summary>
        /// <returns>Status information about the Tika service</returns>
        // GET: api/tika/health
        [HttpGet("health")]
        public async Task<IActionResult> CheckHealth()
        {
            try
            {
                var result = await _tikaService.CheckHealthAsync().ConfigureAwait(false);

                var response = new
                {
                    service = "Apache Tika",
                    status = result.Status.ToString(),
                    description = result.Description,
                    timestamp = DateTimeOffset.UtcNow
                };

                if (result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
                {
                    return Ok(response);
                }
                else if (result.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
                {
                    // Still return 200 but with a degraded status in the body
                    return Ok(response);
                }
                else
                {
                    return StatusCode(503, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Tika health");
                return StatusCode(500, new
                {
                    service = "Apache Tika",
                    status = "Unhealthy",
                    description = $"Error checking Tika health: {ex.Message}",
                    timestamp = DateTimeOffset.UtcNow
                });
            }
        }
    }

}


