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
            try
            {
                // Try to create directory robustly
                if (!Directory.Exists(textOutputDir))
                {
                    Directory.CreateDirectory(textOutputDir);
                }
            }
            catch (Exception dirEx)
            {
                // Write a file in temp if directory creation fails
                var tempPath = Path.Combine(Path.GetTempPath(), "error_create_documents2.txt");
                await File.WriteAllTextAsync(tempPath, $"Failed to create directory {textOutputDir}: {dirEx.Message}").ConfigureAwait(false);
            }

            // Write a debug file to confirm entry into GenerateAsync
            var debugPath = Path.Combine(textOutputDir, "ollama_debug_entry.txt");
            try
            {
                await File.AppendAllTextAsync(debugPath, $"Entered GenerateAsync at {DateTime.UtcNow}\n").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Ignore debug file errors
            }

            var logPath = Path.Combine(textOutputDir, "ollama_log.txt");
            try
            {
                using (var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var logWriter = new StreamWriter(logStream))
                {
                    await logWriter.WriteLineAsync($"🧠 Using model: {_model}").ConfigureAwait(false);
                    await logWriter.WriteLineAsync($"Prompt: {prompt}").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_write_log.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to write log: {ex.Message}").ConfigureAwait(false);
            }

            var promptPath = Path.Combine(textOutputDir, "prompt.txt");
            try
            {
                using (var stream = new FileStream(promptPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(prompt).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_write_prompt.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to write prompt: {ex.Message}").ConfigureAwait(false);
            }

            var payload = new
            {
                model = _model,
                prompt = prompt,
                stream = false
            };
            Console.WriteLine($"🧠 Using model: {_model}");
            var modelPath = Path.Combine(textOutputDir, "model.txt");
            try
            {
                using (var modelStream = new FileStream(modelPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var modelWriter = new StreamWriter(modelStream))
                {
                    await modelWriter.WriteAsync(_model).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_write_model.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to write model: {ex.Message}").ConfigureAwait(false);
            }

            var json = JsonSerializer.Serialize(payload);
            var payloadPath = Path.Combine(textOutputDir, "request_payload.json");
            try
            {
                using (var payloadStream = new FileStream(payloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var payloadWriter = new StreamWriter(payloadStream))
                {
                    await payloadWriter.WriteAsync(json).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_write_payload.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to write payload: {ex.Message}").ConfigureAwait(false);
            }

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var contentTypePath = Path.Combine(textOutputDir, "request_content_type.txt");
            try
            {
                using (var ctStream = new FileStream(contentTypePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var ctWriter = new StreamWriter(ctStream))
                {
                    await ctWriter.WriteAsync(httpContent.Headers.ContentType?.ToString() ?? "").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_write_content_type.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to write content type: {ex.Message}").ConfigureAwait(false);
            }

            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/generate", httpContent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_http_post.txt");
                await File.WriteAllTextAsync(errorPath, $"HTTP POST failed: {ex.Message}").ConfigureAwait(false);
                return string.Empty;
            }

            if (response == null)
            {
                var errorPath = Path.Combine(textOutputDir, "error_response_null.txt");
                await File.WriteAllTextAsync(errorPath, "HTTP response was null").ConfigureAwait(false);
                return string.Empty;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorPath = Path.Combine(textOutputDir, "error_response_status.txt");
                await File.WriteAllTextAsync(errorPath, $"HTTP response status: {response.StatusCode}").ConfigureAwait(false);
                return string.Empty;
            }

            string rawResponse = string.Empty;
            try
            {
                rawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_read_response.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to read response: {ex.Message}").ConfigureAwait(false);
                return string.Empty;
            }

            var rawResponsePath = Path.Combine(textOutputDir, "ollama_raw_response.json");
            try
            {
                using (var rawStream = new FileStream(rawResponsePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (var rawWriter = new StreamWriter(rawStream))
                {
                    await rawWriter.WriteAsync(rawResponse).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorPath = Path.Combine(textOutputDir, "error_write_raw_response.txt");
                await File.WriteAllTextAsync(errorPath, $"Failed to write raw response: {ex.Message}").ConfigureAwait(false);
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
