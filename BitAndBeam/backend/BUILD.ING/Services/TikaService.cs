using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

                var response = await _client.PutAsync("http://tika:9998/tika", content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    _logger.LogError("Tika text extraction failed: {Status} {Reason}", response.StatusCode, response.ReasonPhrase);
                    // Fallback response
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Tika text extraction.");
                return "An unexpected error occurred during document extraction.";
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
                content.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");

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
