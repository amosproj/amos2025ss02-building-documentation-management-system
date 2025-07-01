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


        private static string CategoriesJsonPath => Path.Combine("/app/resources", "document_categories.json");

        private int GetCurrentUserOrganizationId()
        {
            return int.Parse(User.Claims.First(c => c.Type == "org").Value);
        }

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

            // ╭──────────────────────────── 1. save upload ───────────────────────────╮
            var uploadsPath = Path.Combine("/app/documents");
            Directory.CreateDirectory(uploadsPath);

            var fullPath = Path.Combine(uploadsPath, file.FileName);
            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs).ConfigureAwait(false);
            }

            // Reset the file stream position to zero before re-reading
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            using (var fileStream = file.OpenReadStream())
            {
                await fileStream.CopyToAsync(ms).ConfigureAwait(false);
                fileBytes = ms.ToArray();
            }

            // ╭──────────────────────────── 2. Tika extract ───────────────────────────╮
            string metadata = "{}";
            string textForOllama = string.Empty;

            try
            {
                metadata = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName).ConfigureAwait(false);
                textForOllama = await _tikaService.ExtractTextAsync(fileBytes, file.FileName).ConfigureAwait(false);
                _logger.LogInformation("✅ Metadata & text extracted for {File}", file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Tika extraction failed for {File}", file.FileName);
            }

            // ╭──────────────── 3. build prompt (address + category) ─────────────────╮
            var shortText = textForOllama.Length > 4_000 ? textForOllama[..4_000] : textForOllama;
            var categoriesJson = JsonSerializer.Serialize(ReadCategories().Select(c => c.Name));

            var prompt = $$"""
            You are an intelligent document analyzer.

            Given the **extracted text** and a list of **categories** from a German document, your task is to analyze and extract the following fields in a JSON format:
            
            **Example output:**
            {
                "address":
                {
                    "street":"<string|null>",
                    "house_number":"<string|null>",
                    "zip_code":"<string|null>",
                    "city":"<string|null>"
                },
                "category":"<string|null>"
            }

            **TASK A** → Extract a **address** if present.  
            Look for labels like: 
            "Adresse", "Anschrift", "Standort", "Objektadresse", "Gebäudeadresse", "Hausanschrift",
            "Liegenschaft", "Baustellenadresse", "Postanschrift", "Immobilienadresse",
            or field names such as "Straße", "Haus-Nr.", "PLZ", "Ort", and the same terms in free text.

            **TASK B** → Choose the SINGLE best-matching **category** from "categories"
            (use null if none fits)

            **Rules**  
            • Every value must be a JSON string or null - no units, no comments.
            • No markdown, code-fences, or extra keys.
            • Output must parse with `JSON.parse()`.

            "categories":
            {{categoriesJson}}

            "Extracted Text:":
            {{shortText}}
            """;

            Dictionary<string, string>? parsedAddress = null;
            string? matchedCategory = null;
            Building? matchedBuilding = null;

            // ╭──────────────────────────── 4. call Ollama ───────────────────────────╮
            try
            {
                var client = httpClientFactory.CreateClient("Ollama");
                var payload = JsonSerializer.Serialize(new { prompt });
                var resp = await client.PostAsync(
                                "http://ollama:8000/api/Ollama/ask",
                                new StringContent(payload, Encoding.UTF8, "application/json"))
                                        .ConfigureAwait(false);

                if (resp.IsSuccessStatusCode)
                {
                    var jsonStr = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var ollama = JsonSerializer.Deserialize<OllamaController.OllamaResponse>(
                                    jsonStr,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("OLLAMA response field:\n{0}", ollama?.Response);


                    if (!string.IsNullOrWhiteSpace(ollama?.Response))
                    {
                        // ────────  Ollama JSON fence-strip  ────────────────
                        var cleanedJson = ollama.Response
                            .Replace("```json", "", StringComparison.OrdinalIgnoreCase) // remove fenced block tag
                            .Replace("```", "", StringComparison.OrdinalIgnoreCase) // remove any back-ticks
                            .Trim();                                                   // trim spaces / \n etc.

                        int first = cleanedJson.IndexOf('{');
                        int last = cleanedJson.LastIndexOf('}');
                        if (first >= 0 && last > first)
                            cleanedJson = cleanedJson[first..(last + 1)];

                        _logger.LogInformation("🧼 Cleaned Ollama JSON: {Cleaned}", cleanedJson);

                        var root = JsonDocument.Parse(cleanedJson).RootElement;

                        // ------ A. ADDRESS -------
                        if (root.TryGetProperty("address", out var addrObj) &&
                            addrObj.ValueKind == JsonValueKind.Object)
                        {
                            // nested form
                            parsedAddress = new Dictionary<string, string>
                            {
                                ["street"] = addrObj.GetProperty("street").GetString() ?? "",
                                ["house_number"] = addrObj.GetProperty("house_number").GetString() ?? "",
                                ["zip_code"] = addrObj.GetProperty("zip_code").GetString() ?? "",
                                ["city"] = addrObj.GetProperty("city").GetString() ?? ""
                            };
                        }
                        else
                        {
                            // flat fallback
                            parsedAddress = new Dictionary<string, string>
                            {
                                ["street"] = root.TryGetProperty("street", out var s) ? s.GetString() ?? "" : "",
                                ["house_number"] = root.TryGetProperty("house_number", out var hn) ? hn.GetString() ?? "" : "",
                                ["zip_code"] = root.TryGetProperty("zip_code", out var z) ? z.GetString() ?? "" : "",
                                ["city"] = root.TryGetProperty("city", out var c) ? c.GetString() ?? "" : ""
                            };
                        }
                        if (parsedAddress.Values.All(string.IsNullOrWhiteSpace))
                            parsedAddress = null;

                        // ------ B. CATEGORY -------
                        if (root.TryGetProperty("category", out var catElem) &&
                            catElem.ValueKind == JsonValueKind.String)
                        {
                            var cat = catElem.GetString();
                            if (!string.IsNullOrWhiteSpace(cat) &&
                                !string.Equals(cat, "null", StringComparison.OrdinalIgnoreCase))
                                matchedCategory = cat.Trim();
                        }
                    }
                }
                else _logger.LogWarning("⚠️ Ollama service failed (status {Code})", resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ollama analysis failed");
            }

            // ╭────────────── 5. try to map address → building ───────────╮
            if (parsedAddress != null)
            {
                parsedAddress.TryGetValue("street", out var street);
                parsedAddress.TryGetValue("zip_code", out var zip);
                parsedAddress.TryGetValue("house_number", out var house);
                parsedAddress.TryGetValue("city", out var city);

                var orgId = GetCurrentUserOrganizationId();
                var buildings = _context.Buildings
                    .Where(b => b.OrganizationId == orgId)
                    .ToList();
                foreach (var b in buildings)
                {
                    bool okStreet = string.IsNullOrWhiteSpace(street) ||
                                    string.Equals(b.StreetName?.Trim(), street.Trim(),
                                                StringComparison.OrdinalIgnoreCase);

                    bool okZip = string.IsNullOrWhiteSpace(zip) ||
                                    string.Equals(b.PostalCode?.Trim(), zip.Trim(),
                                                StringComparison.OrdinalIgnoreCase);

                    bool okHouse = string.IsNullOrWhiteSpace(house) ||
                                    string.Equals(b.HouseNumber?.Trim(), house.Trim(),
                                                StringComparison.OrdinalIgnoreCase);

                    bool okCity = string.IsNullOrWhiteSpace(city) ||
                                    string.Equals(b.City?.Trim(), city.Trim(),
                                                StringComparison.OrdinalIgnoreCase);

                    if (okStreet && okZip && okHouse && okCity)
                    {
                        matchedBuilding = b;
                        break;
                    }
                }
            }

            // ╭────────────── 6. Try to map matchedCategory (string) to actual category object ───────────╮
            var allCategories = ReadCategories();
            var categoryMatch = allCategories.FirstOrDefault(c =>
                string.Equals(c.Name?.Trim(), matchedCategory, StringComparison.OrdinalIgnoreCase));
            string? matchedCategoryName = categoryMatch?.Name;
            if (categoryMatch == null)
            {
                matchedCategoryName = null;
            }


            // ╭──────────────────────────── 7. persist ───────────────────────────────╮
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
                KeyInformation = null, // This could be populated with structured data from document analysis
                UploadedAt = DateTime.UtcNow,
                UploadedBy = null,
                OrganizationId = GetCurrentUserOrganizationId(),
                BuildingId = matchedBuilding?.BuildingId,
                CategoryName = matchedCategoryName
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // ╭──────────────────────────── 8. response ──────────────────────────────╮
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/documents/{document.FileName}";

            return Ok(new
            {
                document.DocumentId,
                FileUrl = fileUrl,
                HasMetadata = metadata != "{}",
                SuggestedAddress = parsedAddress != null &&
                                parsedAddress.Values.Any(v => !string.IsNullOrWhiteSpace(v))
                                ? parsedAddress
                                : new Dictionary<string, string>
                                    {
                                        { "street",       "Couldn't identify" },
                                        { "house_number", "Couldn't identify" },
                                        { "zip_code",     "Couldn't identify" },
                                        { "city",         "Couldn't identify" }
                                    },
                BuildingId = matchedBuilding?.BuildingId,
                BuildingName = matchedBuilding?.Name,
                CategoryName = matchedCategoryName
            });
        }


        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var documents = _context.Documents
                .Where(d => d.OrganizationId == orgId &&
                            (!d.BuildingId.HasValue || buildingIds.Contains(d.BuildingId.Value)))
                .ToList();

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
                KeyInformation = document.KeyInformation,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                OrganizationId = document.OrganizationId
            }).ToList();
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById(int id)
        {
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var document = _context.Documents
                .FirstOrDefault(d =>
                    d.DocumentId == id &&
                    d.OrganizationId == orgId &&
                    (d.BuildingId == null || buildingIds.Contains(d.BuildingId.Value)));
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
                KeyInformation = document.KeyInformation,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                OrganizationId = document.OrganizationId
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
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var document = _context.Documents
                .FirstOrDefault(d =>
                    d.DocumentId == id &&
                    d.OrganizationId == orgId &&
                    (d.BuildingId == null || buildingIds.Contains(d.BuildingId.Value)));


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
                KeyInformation = document.KeyInformation,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                OrganizationId = document.OrganizationId
            };
            return Ok(dto);
        }

        [HttpPatch("{id}")]
        public IActionResult UpdateDocumentMetadata(int id, [FromBody] DocumentMetadataPatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var document = _context.Documents
                .FirstOrDefault(d =>
                    d.DocumentId == id &&
                    d.OrganizationId == orgId &&
                    (d.BuildingId == null || buildingIds.Contains(d.BuildingId.Value)));


            if (document == null)
                return NotFound();

            // Handle CategoryName logic (creation removed)
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
                if (!buildingIds.Contains(request.BuildingId.Value))
                    return BadRequest($"Building with ID {request.BuildingId} not found in current organization.");
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
                KeyInformation = document.KeyInformation,
                FileName = document.FileName,
                UploadedAt = document.UploadedAt,
                OrganizationId = document.OrganizationId
            };
            return Ok(dto);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteDocument(int id)
        {
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var document = _context.Documents
                .FirstOrDefault(d =>
                    d.DocumentId == id &&
                    d.OrganizationId == orgId &&
                    (d.BuildingId == null || buildingIds.Contains(d.BuildingId.Value)));
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
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var document = _context.Documents
                .FirstOrDefault(d =>
                    d.DocumentId == id &&
                    d.OrganizationId == orgId &&
                    (d.BuildingId == null || buildingIds.Contains(d.BuildingId.Value)));


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
            var orgId = GetCurrentUserOrganizationId();
            var buildingIds = _context.Buildings
                .Where(b => b.OrganizationId == orgId)
                .Select(b => b.BuildingId)
                .ToList();

            var document = _context.Documents
                .FirstOrDefault(d =>
                    d.DocumentId == id &&
                    d.OrganizationId == orgId &&
                    (d.BuildingId == null || buildingIds.Contains(d.BuildingId.Value)));
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

}
