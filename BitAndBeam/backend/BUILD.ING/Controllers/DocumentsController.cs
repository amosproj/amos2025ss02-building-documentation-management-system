using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BUILD.ING.Data;
using BUILD.ING.Models;
using BUILD.ING.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly TikaService _tikaService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(AppDbContext context, IWebHostEnvironment env, TikaService tikaService, ILogger<DocumentsController> logger)
        {
            Console.WriteLine("🚀 DocumentsController loaded");
            _context = context;
            _env = env;
            _tikaService = tikaService;
            _logger = logger;
        }

        private string GetCurrentUserGroupId()
        {
            return "group2"; // Hardcoded for now
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file, [FromServices] IHttpClientFactory httpClientFactory)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var uploadsPath = Path.Combine("/app/documents");
            Directory.CreateDirectory(uploadsPath);

            var fullPath = Path.Combine(uploadsPath, file.FileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream).ConfigureAwait(false);
            }

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms).ConfigureAwait(false);
                fileBytes = ms.ToArray();
            }

            string metadata = "{}";
            string textForOllama = string.Empty;

            try
            {
                metadata = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName).ConfigureAwait(false);
                textForOllama = await _tikaService.ExtractTextAsync(fileBytes, file.FileName).ConfigureAwait(false);
                _logger.LogInformation("✅ Metadata and text extracted for {FileName}", file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Tika extraction failed for file {FileName}", file.FileName);
            }

            Dictionary<string, string>? parsedAddress = null;
            Building? matchedBuilding = null;

            try
            {
                var shortText = textForOllama.Length > 3000 ? textForOllama.Substring(0, 3000) : textForOllama;

                var prompt = $$"""
                The following is the extracted text from a German document. Your task is to identify if it contains an address under the field "Adresse" or in free text.

                Extract the address **only if it looks like a valid building address**, and return the result in JSON format with the following 4 fields:

                - street
                - house_number
                - zip_code
                - city

                All values must be strings or null. DO NOT return markdown or explanation.

                Example:
                {
                    "street": "Riedener Str.",
                    "house_number": "1a",
                    "zip_code": "90518",
                    "city": "Altdorf"
                }

                Text:
                {{shortText}}
                """;

                var client = httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(new { prompt });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://ollama:8000/api/Ollama/ask", content).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var ollamaResultJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var ollamaResult = JsonSerializer.Deserialize<OllamaController.OllamaResponse>(
                        ollamaResultJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    parsedAddress = JsonSerializer.Deserialize<Dictionary<string, string>>(ollamaResult?.Response ?? "{}", new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsedAddress != null && parsedAddress.TryGetValue("street", out var street) &&
                        parsedAddress.TryGetValue("zip_code", out var zipCode))
                    {
                        parsedAddress.TryGetValue("house_number", out var houseNumber);
                        parsedAddress.TryGetValue("city", out var city);

                        var buildings = _context.Buildings.ToList();
                        foreach (var building in buildings)
                        {
                            bool matchesStreet = string.Equals(building.StreetName?.Trim(), street?.Trim(), StringComparison.OrdinalIgnoreCase);
                            bool matchesZip = string.Equals(building.PostalCode?.Trim(), zipCode?.Trim(), StringComparison.OrdinalIgnoreCase);
                            bool matchesHouse = string.IsNullOrWhiteSpace(houseNumber) ||
                                string.Equals(building.HouseNumber?.Trim(), houseNumber?.Trim(), StringComparison.OrdinalIgnoreCase);
                            bool matchesCity = string.IsNullOrWhiteSpace(city) ||
                                string.Equals(building.City?.Trim(), city?.Trim(), StringComparison.OrdinalIgnoreCase);

                            if (matchesStreet && matchesZip && matchesHouse && matchesCity)
                            {
                                matchedBuilding = building;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Ollama service failed to respond.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ollama analysis failed");
            }

            var document = new Document
            {
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = file.FileName,
                FilePath = file.FileName,
                FileType = Path.GetExtension(file.FileName)?.TrimStart('.')?.ToLower() ?? "unknown",
                FileSize = (int) file.Length,
                UploadDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1.0",
                Status = "draft",
                IsPublic = false,
                Description = "No description provided",
                Metadata = metadata,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = null,
                GroupId = GetCurrentUserGroupId()
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/documents/{document.FileName}";

            return Ok(new
            {
                document.DocumentId,
                FileUrl = fileUrl,
                HasMetadata = metadata != "{}",
                SuggestedAddress = parsedAddress != null && parsedAddress.Values.Any(v => !string.IsNullOrWhiteSpace(v))
                    ? parsedAddress
                    : new Dictionary<string, string>
                    {
                        { "street", "Couldn't identify" },
                        { "house_number", "Couldn't identify" },
                        { "zip_code", "Couldn't identify" },
                        { "city", "Couldn't identify" }
                    },
                DetectedBuilding = matchedBuilding != null
                    ? new
                    {
                        matchedBuilding.BuildingId,
                        matchedBuilding.StreetName,
                        matchedBuilding.HouseNumber,
                        matchedBuilding.PostalCode,
                        matchedBuilding.City
                    }
                    : null
            });
        }

        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            var groupId = GetCurrentUserGroupId();
            var documents = _context.Documents.Where(d => d.GroupId == groupId).ToList();
            var categoryMap = _context.DocumentCategories.ToDictionary(c => c.CategoryId, c => c.Name);
            var buildingMap = _context.Buildings.ToDictionary(b => b.BuildingId, b => b.Name);
            var dtos = documents.Select(document => new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryId = document.CategoryId,
                CategoryName = document.CategoryId.HasValue && categoryMap.ContainsKey(document.CategoryId.Value) ? categoryMap[document.CategoryId.Value] : null,
                BuildingId = document.BuildingId,
                BuildingName = document.BuildingId.HasValue && buildingMap.ContainsKey(document.BuildingId.Value) ? buildingMap[document.BuildingId.Value] : null,
                UploadedBy = document.UploadedBy,
                UploadDate = document.UploadDate,
                LastModified = document.LastModified,
                Version = document.Version,
                Status = document.Status,
                Description = document.Description,
                IsPublic = document.IsPublic,
                Metadata = document.Metadata,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                GroupId = document.GroupId
            }).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById(int id)
        {
            var groupId = GetCurrentUserGroupId();
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == groupId);
            if (document == null)
                return NotFound();
            var categoryName = document.CategoryId.HasValue
                ? _context.DocumentCategories.Where(c => c.CategoryId == document.CategoryId.Value).Select(c => c.Name).FirstOrDefault()
                : null;
            var buildingName = document.BuildingId.HasValue
                ? _context.Buildings.Where(b => b.BuildingId == document.BuildingId.Value).Select(b => b.Name).FirstOrDefault()
                : null;
            var dto = new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryId = document.CategoryId,
                CategoryName = categoryName,
                BuildingId = document.BuildingId,
                BuildingName = buildingName,
                UploadedBy = document.UploadedBy,
                UploadDate = document.UploadDate,
                LastModified = document.LastModified,
                Version = document.Version,
                Status = document.Status,
                Description = document.Description,
                IsPublic = document.IsPublic,
                Metadata = document.Metadata,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                GroupId = document.GroupId
            };
            return Ok(dto);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateDocument(int id, [FromBody] DocumentUpdateRequest request)
        {
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            // Handle Category lookup or creation
            if (!string.IsNullOrEmpty(request.Category))
            {
                var category = _context.DocumentCategories.FirstOrDefault(c => c.Name == request.Category);
                if (category == null)
                {
                    category = new DocumentCategory { Name = request.Category };
                    _context.DocumentCategories.Add(category);
                    _context.SaveChanges(); // Save to get the generated CategoryId
                }
                document.CategoryId = category.CategoryId;
                document.Category = category;
                if (category.Documents == null)
                    category.Documents = new List<Document>();
                if (!category.Documents.Contains(document))
                    category.Documents.Add(document); // Ensure the document is linked to the category
            }

            // Handle Building lookup
            if (!string.IsNullOrEmpty(request.Building))
            {
                var building = _context.Buildings.FirstOrDefault(b => b.Name == request.Building);
                if (building == null)
                    return BadRequest($"Building '{request.Building}' not found.");
                document.BuildingId = building.BuildingId;
                document.Building = building;
                if (building.Documents == null)
                    building.Documents = new List<Document>();
                if (!building.Documents.Contains(document))
                    building.Documents.Add(document); // Ensure the document is linked to the building
            }

            var requestType = request.GetType();
            var documentType = document.GetType();
            foreach (var reqProp in requestType.GetProperties())
            {
                if (reqProp.Name == "Category" || reqProp.Name == "Building")
                    continue; // Already handled above
                var value = reqProp.GetValue(request);
                if (value == null)
                    continue; // Skip nulls to keep original value
                var docProp = documentType.GetProperty(reqProp.Name);
                if (docProp == null || !docProp.CanWrite)
                {
                    return BadRequest($"Field '{reqProp.Name}' does not exist on Document.");
                }
                if (!docProp.PropertyType.IsAssignableFrom(reqProp.PropertyType))
                {
                    return BadRequest($"Type mismatch for field '{reqProp.Name}': expected {docProp.PropertyType.Name}, got {reqProp.PropertyType.Name}.");
                }
                docProp.SetValue(document, value);
            }

            _context.SaveChanges();
            // Reload navigation properties to ensure up-to-date values
            _context.Entry(document).Reference(d => d.Category).Load();
            _context.Entry(document).Reference(d => d.Building).Load();

            var dto = new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryId = document.CategoryId,
                CategoryName = document.Category?.Name,
                BuildingId = document.BuildingId,
                BuildingName = document.Building?.Name,
                UploadedBy = document.UploadedBy,
                UploadDate = document.UploadDate,
                LastModified = document.LastModified,
                Version = document.Version,
                Status = document.Status,
                Description = document.Description,
                IsPublic = document.IsPublic,
                Metadata = document.Metadata,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                GroupId = document.GroupId
            };
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteDocument(int id)
        {
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            var filePath = Path.Combine("/app/documents", document.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Console.WriteLine($"✅ File deleted: {filePath}");
            }
            else
            {
                Console.WriteLine($"⚠️ File not found at: {filePath}");
            }

            _context.Documents.Remove(document);
            _context.SaveChanges();

            return NoContent();
        }

        [HttpGet("{id}/download")]
        public IActionResult DownloadDocument(int id)
        {
            var groupId = GetCurrentUserGroupId();
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == groupId);
            if (document == null)
                return NotFound();

            var filePath = Path.Combine("/app/documents", document.FileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/octet-stream", document.FileName);
        }

        [HttpGet("{id}/preview")]
        public IActionResult PreviewDocument(int id)
        {
            var groupId = GetCurrentUserGroupId();
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == groupId);
            if (document == null)
                return NotFound();

            var filePath = Path.Combine("/app/documents", document.FileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Determine content type based on file extension
            string contentType = document.FileType switch
            {
                "pdf" => "application/pdf",
                "png" => "image/png",
                "jpg" => "image/jpeg",
                "jpeg" => "image/jpeg",
                _ => "application/octet-stream" // fallback
            };

            return File(fileBytes, contentType);
        }
        [HttpPatch("{id}")]
        public IActionResult UpdateDocumentMetadata(int id, [FromBody] DocumentMetadataPatchRequest request)
        {
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            if (request.CategoryId.HasValue)
            {
                var category = _context.DocumentCategories.FirstOrDefault(c => c.CategoryId == request.CategoryId.Value);
                if (category == null)
                    return BadRequest($"Category with ID {request.CategoryId} not found.");
                document.Category = category;
                document.CategoryId = request.CategoryId.Value;
                if (category.Documents == null)
                    category.Documents = new List<Document>();
                if (!category.Documents.Contains(document))
                    category.Documents.Add(document); // Ensure the document is linked to the category
            }
            if (request.BuildingId.HasValue)
            {
                var building = _context.Buildings.FirstOrDefault(b => b.BuildingId == request.BuildingId.Value);
                if (building == null)
                    return BadRequest($"Building with ID {request.BuildingId} not found.");
                document.Building = building;
                document.BuildingId = request.BuildingId.Value;
                if (building.Documents == null)
                    building.Documents = new List<Document>();
                if (!building.Documents.Contains(document))
                    building.Documents.Add(document); // Ensure the document is linked to the building
            }

            _context.SaveChanges();

            var dto = new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryId = document.CategoryId,
                CategoryName = document.Category?.Name,
                BuildingId = document.BuildingId,
                BuildingName = document.Building?.Name,
                UploadedBy = document.UploadedBy,
                UploadDate = document.UploadDate,
                LastModified = document.LastModified,
                Version = document.Version,
                Status = document.Status,
                Description = document.Description,
                IsPublic = document.IsPublic,
                Metadata = document.Metadata,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                GroupId = document.GroupId
            };
            return Ok(dto);
        }

        public class DocumentMetadataPatchRequest
        {
            public int? CategoryId { get; set; }
            public int? BuildingId { get; set; }
        }
    }
}
