using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BitAndBeam.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaBaseUrl;
        private readonly string _model;
        public OllamaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _ollamaBaseUrl = configuration["Ollama:BaseUrl"];
            _model = configuration["Ollama:Model"];
        }

        public async Task<string> GenerateAsync(string prompt)
        {
            prompt = prompt.Replace("\r\n", "\n"); // Normalize
            var textOutputDir = "/app/documents2";
            // Ensure the directory exists for storing text files
            Directory.CreateDirectory(textOutputDir);
            var promptPath = Path.Combine(textOutputDir, "prompt.txt");
            using (var stream = new FileStream(promptPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(prompt).ConfigureAwait(false);
            }
            var payload = new
            {
                model = _model,
                prompt = prompt,
                stream = false
            };
            Console.WriteLine($"🧠 Using model: {_model}");
            var modelPath = Path.Combine(textOutputDir, "model.txt");
            using (var modelStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var modelWriter = new StreamWriter(modelStream))
            {
                await modelWriter.WriteAsync(_model).ConfigureAwait(false);
            }

            var json = JsonSerializer.Serialize(payload);

            var payloadPath = Path.Combine(textOutputDir, "request_payload.json");
            using (var payloadStream = new FileStream(payloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var payloadWriter = new StreamWriter(payloadStream))
            {
                await payloadWriter.WriteAsync(json).ConfigureAwait(false);
            }
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Save Content-Type header to /app/documents2/request_content_type.txt
            var contentTypePath = Path.Combine(textOutputDir, "request_content_type.txt");
            using (var ctStream = new FileStream(contentTypePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var ctWriter = new StreamWriter(ctStream))
            {
                await ctWriter.WriteAsync(httpContent.Headers.ContentType?.ToString() ?? "").ConfigureAwait(false);
            }

            var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", httpContent).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var rawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            // Save rawResponse to /app/documents2/ollama_raw_response.json
            var rawResponsePath = Path.Combine(textOutputDir, "ollama_raw_response.json");
            using (var rawStream = new FileStream(rawResponsePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            using (var rawWriter = new StreamWriter(rawStream))
            {
                await rawWriter.WriteAsync(rawResponse).ConfigureAwait(false);
            }
            return rawResponse;
        }

        public async Task<bool> CheckHealthAsync()
        {
            var response = await _httpClient.GetAsync($"{_ollamaBaseUrl}/api/tags").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
    }
}
