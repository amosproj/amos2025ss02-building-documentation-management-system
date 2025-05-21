using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OllamaController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public OllamaController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public class OllamaRequest
        {
            public string Prompt { get; set; }
            public object Context { get; set; } // optional
        }

        public class OllamaResponse
        {
            public string Response { get; set; }
            public long ResponseTimeMs { get; set; }
        }

        /// <summary>
        /// Sends prompt to Ollama backend and returns response with metadata.
        /// </summary>
        /// <param name="request">Prompt and optional context</param>
        /// <returns>Structured response with metadata</returns>
        [HttpPost("ask")]
        public async Task<IActionResult> AskOllama([FromBody] OllamaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { error = "Prompt is required." });
            }

            var stopwatch = Stopwatch.StartNew();

            var payload = new
            {
                prompt = request.Prompt
                // context can be added here if needed in the future
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://ollama:8000/ask", httpContent).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int) response.StatusCode, new { error = "Ollama service error" });
                }

                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                stopwatch.Stop();

                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                ollamaResponse.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

                return Ok(ollamaResponse);
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { error = "Ollama service unreachable" });
            }
        }
    }
}
