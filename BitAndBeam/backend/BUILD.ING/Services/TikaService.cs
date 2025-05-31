using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitAndBeam.Tika
{
    public class TikaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _tikaServerUrl;
        private readonly ILogger<TikaService> _logger;
        private readonly int _timeout = 30; // seconds

        public TikaService(IConfiguration configuration, ILogger<TikaService> logger, HttpClient httpClient = null)
        {
            _logger = logger;
            _tikaServerUrl = configuration["Tika:ServerUrl"] ?? "http://tika:9998";
            
            // Use the provided client or create a new one
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeout);
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));
        }

        /// <summary>
        /// Extracts text from a document using Apache Tika
        /// </summary>
        /// <param name="fileBytes">The binary content of the file</param>
        /// <param name="fileName">The name of the file (used for error reporting)</param>
        /// <returns>Extracted text or error message</returns>
        public async Task<string> ExtractTextAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                _logger.LogInformation($"Extracting text from {fileName} ({fileBytes.Length} bytes)");
                
                // Create the request content with the file bytes
                var content = new ByteArrayContent(fileBytes);
                
                // Send the request to Tika's text extraction endpoint
                var response = await _httpClient.PostAsync($"{_tikaServerUrl}/tika/text", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var extractedText = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully extracted {extractedText.Length} characters from {fileName}");
                    return extractedText;
                }
                else
                {
                    _logger.LogError($"Tika extraction failed with status code {response.StatusCode} for file {fileName}");
                    
                    // Map HTTP status codes to appropriate error messages
                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.UnsupportedMediaType => "Could not extract text from the document.",
                        System.Net.HttpStatusCode.ServiceUnavailable => "Document extraction service is currently unavailable.",
                        System.Net.HttpStatusCode.GatewayTimeout => "Document extraction timed out. Please try again.",
                        _ => "An unexpected error occurred during document extraction."
                    };
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning($"Request to Tika server timed out after {_timeout} seconds for file {fileName}");
                return "Document extraction timed out. Please try again.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Failed to connect to Tika server for file {fileName}");
                return "Document extraction service is currently unavailable.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during text extraction for file {fileName}");
                return "An unexpected error occurred during document extraction.";
            }
        }

        /// <summary>
        /// Extracts metadata from a document using Apache Tika
        /// </summary>
        /// <param name="fileBytes">The binary content of the file</param>
        /// <param name="fileName">The name of the file (used for error reporting)</param>
        /// <returns>JSON string containing metadata or error message</returns>
        public async Task<string> ExtractMetadataAsync(byte[] fileBytes, string fileName)
        {
            try
            {
                _logger.LogInformation($"Extracting metadata from {fileName} ({fileBytes.Length} bytes)");
                
                // Create the request content with the file bytes
                var content = new ByteArrayContent(fileBytes);
                
                // Send the request to Tika's metadata extraction endpoint
                var response = await _httpClient.PostAsync($"{_tikaServerUrl}/tika/rmeta", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var extractedMetadata = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Successfully extracted metadata from {fileName}");
                    return extractedMetadata;
                }
                else
                {
                    _logger.LogError($"Tika metadata extraction failed with status code {response.StatusCode} for file {fileName}");
                    
                    // Return error message based on status code
                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.UnsupportedMediaType => "Could not extract metadata from the document.",
                        System.Net.HttpStatusCode.ServiceUnavailable => "Document metadata extraction service is currently unavailable.",
                        System.Net.HttpStatusCode.GatewayTimeout => "Document metadata extraction timed out. Please try again.",
                        _ => "An unexpected error occurred during document metadata extraction."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during metadata extraction for file {fileName}");
                return "An unexpected error occurred during document metadata extraction.";
            }
        }
    }
}
