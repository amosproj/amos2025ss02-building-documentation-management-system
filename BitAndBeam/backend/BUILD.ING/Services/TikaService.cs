using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BUILD.ING.Services
{
    public class TikaService
    {
        private readonly HttpClient _client;
        private readonly ILogger<TikaService> _logger;

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
        public async Task<string> ExtractTextAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                using var content = new ByteArrayContent(fileBytes);
                content.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

                var response = await _client.PutAsync("tika", content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    _logger.LogError("Tika extraction failed: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
                    // Fallback response
                    return "Could not extract text from the document.";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Tika server is unreachable.");
                return "Document extraction service is currently unavailable.";
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Tika request timed out.");
                return "Document extraction timed out. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Tika extraction.");
                return "An unexpected error occurred during document extraction.";
            }
        }
    }
}
