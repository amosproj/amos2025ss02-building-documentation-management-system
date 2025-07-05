using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace BitAndBeam.Services
{
    public class TikaService
    {
        private readonly HttpClient _client;
        private readonly ILogger<TikaService> _logger;
        private readonly int _maxDocumentSizeForParallelProcessing = 10 * 1024 * 1024; // 10MB
        private readonly int _optimalChunkSizeForOcr = 1 * 1024 * 1024; // 1MB
        private readonly int _maxParallelTasks = 4; // Maximum number of parallel OCR tasks

        public TikaService(HttpClient client, ILogger<TikaService> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Extracts text from a file using the Tika server. Handles errors and logs them appropriately.
        /// </summary>
        /// <param name="fileBytes">The file contents as a byte array.</param>
        /// <param name="fileName">The file name (for logging).</param>
        /// <returns>Extracted text or a fallback message in case of error.</returns>
        /// <summary>
        /// Backwards-compatible overload that assumes OCR when needed (performOcr=true)
        /// </summary>
        public Task<string> ExtractTextAsync(byte[] fileBytes, string fileName)
        {
            // Default behavior: allow OCR for maximum accuracy
            return ExtractTextAsync(fileBytes, fileName, true);
        }

        public async Task<string> ExtractTextAsync(byte[] fileBytes, string fileName, bool performOcr)
        {
            try
            {
                _logger.LogInformation("Starting text extraction for {FileName}, Size: {Size}KB, OCR: {PerformOcr}", 
                    fileName, fileBytes.Length / 1024, performOcr);
                
                // Quick check for file type to optimize processing
                string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                bool isPdf = fileExtension == ".pdf";
                bool isImage = new[] { ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".gif" }.Contains(fileExtension);
                
                // Fast path: If it's not a PDF or image and performOcr=true, we can skip the two-pass approach
                if (!isPdf && !isImage && performOcr)
                {
                    return await ExtractTextSinglePassAsync(fileBytes, fileName, performOcr);
                }
                
                // For large PDFs and performOcr=true, use parallel processing
                if (isPdf && performOcr && fileBytes.Length > _maxDocumentSizeForParallelProcessing)
                {
                    _logger.LogInformation("Using parallel OCR for large PDF: {FileName}, Size: {Size}KB", 
                        fileName, fileBytes.Length / 1024);
                    return await ExtractTextFromLargeDocumentAsync(fileBytes, fileName);
                }
                
                // Standard case: try extraction with optimization based on file type
                return await ExtractTextSinglePassAsync(fileBytes, fileName, performOcr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Tika text extraction for {FileName}", fileName);
                return "An unexpected error occurred during document extraction.";
            }
        }
        
        private async Task<string> ExtractTextSinglePassAsync(byte[] fileBytes, string fileName, bool performOcr)
        {
            try
            {
                using var content = new ByteArrayContent(fileBytes);
                // Encode the filename properly for Content-Disposition header
                var encodedFileName = Uri.EscapeDataString(fileName);
                content.Headers.Add("Content-Disposition", $"attachment; filename=\"{encodedFileName}\"");
                
                // Add OCR control header when needed
                if (performOcr)
                {
                    // Force OCR with optimized parameters
                    content.Headers.Add("X-Tika-PDFOcrStrategy", "ocr_only");
                }
                else
                {
                    // Explicitly disable OCR for faster processing
                    content.Headers.Add("X-Tika-PDFOcrStrategy", "no_ocr");
                }

                var request = new HttpRequestMessage(HttpMethod.Put, "http://tika:9998/tika")
                {
                    Content = content
                };
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _client.SendAsync(request).ConfigureAwait(false);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogInformation("Tika extraction completed in {ElapsedMs}ms for {FileName}, OCR: {PerformOcr}", 
                        sw.ElapsedMilliseconds, fileName, performOcr);
                    return result;
                }
                else
                {
                    _logger.LogError("Tika text extraction failed: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
                    return "Could not extract text from the document.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Tika server is unreachable for text extraction.");
                return "Document extraction service is currently unavailable.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Tika request timed out for text extraction.");
                return "Document extraction timed out. Please try again.";
            }
        }
        
        /// <summary>
        /// Processes large documents by splitting them into chunks and processing in parallel
        /// </summary>
        private async Task<string> ExtractTextFromLargeDocumentAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                // First try to get document metadata to determine number of pages
                var metadata = await ExtractMetadataAsync(fileBytes, fileName);
                int pageCount = 1;
                
                // Try to parse metadata to get page count
                try
                {   
                    var metadataObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadata);
                    if (metadataObj != null && metadataObj.TryGetValue("xmpTPg:NPages", out JsonElement pageElement) && 
                        pageElement.ValueKind == JsonValueKind.Number)
                    {
                        pageCount = pageElement.GetInt32();
                    }
                    else if (metadataObj != null && metadataObj.TryGetValue("Page-Count", out pageElement) && 
                             pageElement.ValueKind == JsonValueKind.Number)
                    {
                        pageCount = pageElement.GetInt32();
                    }
                }
                catch
                {
                    // If we can't get page count, use a conservative estimate based on file size
                    pageCount = Math.Max(1, fileBytes.Length / (500 * 1024)); // Approx 500KB per page as fallback
                }
                
                _logger.LogInformation("Large document processing: {FileName}, Size: {Size}KB, Estimated pages: {PageCount}", 
                    fileName, fileBytes.Length / 1024, pageCount);
                
                // For very small documents, just process normally
                if (pageCount <= 3)
                {   
                    return await ExtractTextSinglePassAsync(fileBytes, fileName, true);
                }
                
                // For larger documents, use the PDF-aware page extraction
                var results = new List<string>();
                var options = new ParallelOptions { MaxDegreeOfParallelism = _maxParallelTasks };
                var tasks = new List<Task<string>>();
                
                // Process all pages, but limit parallelism
                for (int page = 1; page <= pageCount; page += _maxParallelTasks)
                {
                    // Create a batch of tasks limited by MaxDegreeOfParallelism
                    var batch = new List<Task<string>>();
                    for (int i = 0; i < _maxParallelTasks && page + i <= pageCount; i++)
                    {
                        int currentPage = page + i;
                        batch.Add(ExtractTextFromPageAsync(fileBytes, fileName, currentPage));
                    }
                    
                    // Wait for all tasks in this batch to complete
                    var batchResults = await Task.WhenAll(batch);
                    results.AddRange(batchResults);
                }
                
                // Join all the text results
                return string.Join("\n\n", results.Where(r => !string.IsNullOrEmpty(r) && 
                                                   !r.Contains("Could not extract text") && 
                                                   !r.Contains("Document extraction")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in parallel document processing for {FileName}", fileName);
                // Fallback to regular processing
                return await ExtractTextSinglePassAsync(fileBytes, fileName, true);
            }
        }
        
        /// <summary>
        /// Extracts text from a specific page of a PDF document
        /// </summary>
        private async Task<string> ExtractTextFromPageAsync(byte[] fileBytes, string fileName, int pageNumber)
        {
            try
            {
                using var content = new ByteArrayContent(fileBytes);
                var encodedFileName = Uri.EscapeDataString(fileName);
                content.Headers.Add("Content-Disposition", $"attachment; filename=\"{encodedFileName}\"");
                
                // Add PDF page extraction headers
                content.Headers.Add("X-Tika-PDFOcrStrategy", "ocr_only");
                content.Headers.Add("X-Tika-PDFStartPage", pageNumber.ToString());
                content.Headers.Add("X-Tika-PDFEndPage", pageNumber.ToString());

                var request = new HttpRequestMessage(HttpMethod.Put, "http://tika:9998/tika")
                {
                    Content = content
                };
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await _client.SendAsync(request).ConfigureAwait(false);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogInformation("Page {Page} extraction completed in {ElapsedMs}ms", 
                        pageNumber, sw.ElapsedMilliseconds);
                    return result;
                }
                else
                {
                    _logger.LogError("Page {Page} extraction failed: {Status}", pageNumber, response.StatusCode);
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from page {Page}", pageNumber);
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts metadata from a file using the Tika server. Handles errors and logs them appropriately.
        /// </summary>
        /// <param name="fileBytes">The file contents as a byte array.</param>
        /// <param name="fileName">The file name (for logging).</param>
        /// <returns>Extracted metadata as JSON string or a fallback message in case of error.</returns>
        public async Task<string> ExtractMetadataAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                using var content = new ByteArrayContent(fileBytes);
                // Encode the filename properly for Content-Disposition header
                var encodedFileName = Uri.EscapeDataString(fileName);
                content.Headers.Add("Content-Disposition", $"attachment; filename=\"{encodedFileName}\"");

                var response = await _client.PutAsync("http://tika:9998/meta", content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    _logger.LogError("Tika metadata extraction failed: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
                    // Fallback response
                    return "{}";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Tika server is unreachable for metadata extraction.");
                return "{}";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Tika request timed out for metadata extraction.");
                return "{}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Tika metadata extraction.");
                return "{}";
            }
        }

        /// <summary>
        /// Checks the health of the Tika service
        /// </summary>
        /// <returns>A HealthCheckResult indicating the status of the Tika service</returns>
        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            try
            {
                var response = await _client.GetAsync("http://tika:9998/version").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var version = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return HealthCheckResult.Healthy($"Tika service is healthy. Version: {version}");
                }
                else
                {
                    return HealthCheckResult.Degraded($"Tika service responded with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Tika health");
                return HealthCheckResult.Unhealthy("Unable to communicate with Tika service", ex);
            }
        }
    }
}


