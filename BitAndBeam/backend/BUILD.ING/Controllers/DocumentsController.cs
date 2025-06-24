using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BUILD.ING.Data;
using BUILD.ING.Models;
using BUILD.ING.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BUILD.ING.Controllers
{
    [Authorize]
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

        private static string CategoriesJsonPath => Path.Combine("/app/resources", "document_categories.json");

        private static List<DocumentCategory> ReadCategories()
        {
            var json = System.IO.File.ReadAllText(CategoriesJsonPath);
            using var doc = JsonDocument.Parse(json);
            var categoriesElem = doc.RootElement.GetProperty("categories");
            var categories = JsonSerializer.Deserialize<List<DocumentCategory>>(categoriesElem.GetRawText()) ?? new();
            return categories;
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
                GroupId = GetCurrentUserGroupId(),
                BuildingId = matchedBuilding?.BuildingId,
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
                BuildingId = matchedBuilding?.BuildingId,
                BuildingName = matchedBuilding?.Name
            });
        }


        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            var groupId = GetCurrentUserGroupId();
            var documents = _context.Documents.Where(d => d.GroupId == groupId).ToList();
            var dtos = documents.Select(document => new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryName = document.CategoryName,
                BuildingId = document.BuildingId,
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
            var dto = new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryName = document.CategoryName,
                BuildingId = document.BuildingId,
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

        [HttpGet("categories")]
        public IActionResult GetDocumentCategories()
        {
            if (!System.IO.File.Exists(CategoriesJsonPath))
                return NotFound("document_categories.json not found");
            var categories = ReadCategories();
            var result = categories.Select(c => new
            {
                name = c.Name,
                description = c.Description,
                fields = c.Fields,
            }).ToList();
            return Ok(result);
        }

        // [HttpPost("categories")]
        // public IActionResult CreateCategory([FromBody] DocumentCategoryCreateRequest request)
        // {
        //     if (string.IsNullOrWhiteSpace(request.Name))
        //         return BadRequest("Category name is required.");

        //     var categories = ReadCategories();
        //     if (categories.Any(c => string.Equals(c.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
        //         return Conflict($"Category with name '{request.Name}' already exists.");

        //     var newCategory = new DocumentCategory
        //     {
        //         Name = request.Name!,
        //         Description = request.Description,
        //         Fields = request.Fields ?? new List<Dictionary<string, string>>()
        //     };
        //     categories.Add(newCategory);

        //     // Write back to JSON
        //     var json = System.IO.File.ReadAllText(CategoriesJsonPath);
        //     using var docJson = JsonDocument.Parse(json);
        //     var newJsonObj = new Dictionary<string, object>();
        //     foreach (var prop in docJson.RootElement.EnumerateObject())
        //     {
        //         if (prop.Name == "categories")
        //             newJsonObj[prop.Name] = categories;
        //         else
        //             newJsonObj[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText()) ?? new object();
        //     }
        //     var newJson = JsonSerializer.Serialize(newJsonObj, CachedJsonSerializerOptions);
        //     System.IO.File.WriteAllText(CategoriesJsonPath, newJson);

        //     return CreatedAtAction(nameof(GetDocumentCategories), new { name = newCategory.Name }, new
        //     {
        //         name = newCategory.Name,
        //         description = newCategory.Description,
        //         fields = newCategory.Fields
        //     });
        // }

        [HttpPut("{id}")]
        public IActionResult UpdateDocument(int id, [FromBody] DocumentUpdateRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            var requestType = request.GetType();
            var documentType = document.GetType();
            foreach (var reqProp in requestType.GetProperties())
            {
                var value = reqProp.GetValue(request);
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

            var dto = new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryName = document.CategoryName,
                BuildingId = document.BuildingId,
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

        [HttpPatch("{id}")]
        public IActionResult UpdateDocumentMetadata(int id, [FromBody] DocumentMetadataPatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            // Handle CategoryName logic (creation removed)
            var categories = ReadCategories();
            if (request.CategoryName != null)
            {
                document.CategoryName = request.CategoryName;
            }
            else
            {
                document.CategoryName = null;
            }

            // Handle BuildingId logic
            if (request.BuildingId.HasValue)
            {
                var building = _context.Buildings.FirstOrDefault(b => b.BuildingId == request.BuildingId.Value);
                if (building == null)
                    return BadRequest($"Building with ID {request.BuildingId} not found.");
                document.BuildingId = request.BuildingId.Value;
            }
            else
            {
                document.BuildingId = null;
            }

            _context.SaveChanges();
            // No longer reload navigation properties for Category or Building

            var dto = new BUILD.ING.Dto.DocumentDto
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                FilePath = document.FilePath,
                FileType = document.FileType,
                FileSize = document.FileSize,
                CategoryName = document.CategoryName,
                BuildingId = document.BuildingId,
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

    }

    public class DocumentMetadataPatchRequest
    {
        public string? CategoryName { get; set; }
        public int? BuildingId { get; set; }
    }

    public class DocumentUpdateRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    // public class DocumentCategoryCreateRequest
    // {
    //     public string Name { get; set; } = string.Empty;
    //     public string? Description { get; set; }
    //     public List<Dictionary<string, string>>? Fields { get; set; }
    // }
}
