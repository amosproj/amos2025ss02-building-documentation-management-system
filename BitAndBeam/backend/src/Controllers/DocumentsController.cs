using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BitAndBeam.Data;
using BitAndBeam.Models;
using BitAndBeam.Services;
using HtmlAgilityPack; // Ensure this package is installed
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitAndBeam.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly TikaService _tikaService;
        private readonly OllamaService _ollamaService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(AppDbContext context, IWebHostEnvironment env, TikaService tikaService, OllamaService ollamaService, ILogger<DocumentsController> logger)
        {
            _context = context;
            _env = env;
            _tikaService = tikaService;
            _ollamaService = ollamaService;
            _logger = logger;
        }

        private static string CategoriesJsonPath => Path.Combine("/app/resources", "document_categories.json");

        private int GetCurrentUserOrganizationId()
        {
            return int.Parse(User.Claims.First(c => c.Type == "org").Value);
        }

        private static List<DocumentCategory> ReadCategories()
        {
            try
            {
                var json = System.IO.File.ReadAllText(CategoriesJsonPath);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    var categories = JsonSerializer.Deserialize<List<DocumentCategory>>(json) ?? new();
                    return categories;
                }
                else if (root.TryGetProperty("categories", out var categoriesElem))
                {
                    var categories = JsonSerializer.Deserialize<List<DocumentCategory>>(categoriesElem.GetRawText()) ?? new();
                    return categories;
                }
                else
                {
                    return new List<DocumentCategory>();
                }
            }
            catch (Exception ex)
            {
                return new List<DocumentCategory>();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            // 1. Save upload
            var uploadsPath = Path.Combine("/app/documents");
            Directory.CreateDirectory(uploadsPath);

            // Ensure the directory exists for storing text files
            var textOutputDir = "/app/documents2";
            Directory.CreateDirectory(textOutputDir);

            var fullPath = Path.Combine(uploadsPath, file.FileName);
            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(fs).ConfigureAwait(false);
            }

            // Read file bytes for Tika
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            using (var fileStream = file.OpenReadStream())
            {
                await fileStream.CopyToAsync(ms).ConfigureAwait(false);
                fileBytes = ms.ToArray();
            }

            // 2. Tika extract (OCR as first step)
            string metadata = "{}";
            string textForOllama = string.Empty;
            try
            {
                metadata = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName).ConfigureAwait(false);
                textForOllama = await _tikaService.ExtractTextAsync(fileBytes, file.FileName).ConfigureAwait(false);

                // OCR fallback if text is missing/short
                if (string.IsNullOrWhiteSpace(textForOllama) || textForOllama.Length < 50)
                {
                    textForOllama = await _tikaService.ExtractTextAsync(fileBytes, file.FileName, true).ConfigureAwait(false);
                }
                _logger.LogInformation("✅ Metadata & text extracted for {File}", file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Tika extraction failed for {File}", file.FileName);
            }

            // 3. Initial prompt - Only extract address and suggest category (NO key information yet)
            textForOllama = ExtractVisibleText(textForOllama);
            // var shortText = textForOllama.Length > 4_000 ? textForOllama[..4_000] : textForOllama;
            var shortText = textForOllama;
            // Read categories directly from file as raw JSON to avoid double serialization
            var categoriesSchemaJson = System.IO.File.ReadAllText(CategoriesJsonPath);

            var initialPrompt = BuildInitialPrompt(shortText, categoriesSchemaJson);

            // Initialize result fields
            Dictionary<string, string>? parsedAddress = null;
            string? suggestedCategory = null;
            Building? matchedBuilding = null;

            // 4. Initial Ollama call - only for address and category suggestion
            try
            {
                var ollamaRawResponse = await _ollamaService.GenerateAsync(initialPrompt).ConfigureAwait(false);
                _logger.LogInformation("OLLAMA initial response:\n{0}", ollamaRawResponse);

                // Parse main response object
                var ollamaJsonDoc = JsonDocument.Parse(ollamaRawResponse);
                var ollamaRoot = ollamaJsonDoc.RootElement;

                if (ollamaRoot.TryGetProperty("response", out var responseElem))
                {
                    var innerResponseString = responseElem.GetString();

                    // Remove possible code-fence
                    var cleanedJson = innerResponseString
                        .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();

                    int first = cleanedJson.IndexOf('{');
                    int last = cleanedJson.LastIndexOf('}');
                    if (first >= 0 && last > first)
                        cleanedJson = cleanedJson[first..(last + 1)];

                    _logger.LogInformation("🧼 Cleaned Ollama JSON: {Cleaned}", cleanedJson);

                    var cleanedJsonPath = Path.Combine(textOutputDir, "initial_ollama_response.json");
                    using (var cleanedStream = new FileStream(cleanedJsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    using (var cleanedWriter = new StreamWriter(cleanedStream))
                    {
                        await cleanedWriter.WriteAsync(cleanedJson).ConfigureAwait(false);
                    }

                    var root = JsonDocument.Parse(cleanedJson).RootElement;

                    // ADDRESS
                    if (root.TryGetProperty("address", out var addrObj) && addrObj.ValueKind == JsonValueKind.Object)
                    {
                        parsedAddress = new Dictionary<string, string>
                        {
                            ["street"] = addrObj.GetProperty("street").GetString() ?? "",
                            ["house_number"] = addrObj.GetProperty("house_number").GetString() ?? "",
                            ["zip_code"] = addrObj.GetProperty("zip_code").GetString() ?? "",
                            ["city"] = addrObj.GetProperty("city").GetString() ?? ""
                        };
                        if (parsedAddress.Values.All(string.IsNullOrWhiteSpace)) parsedAddress = null;
                    }

                    // CATEGORY (only suggestion, not final)
                    if (root.TryGetProperty("category", out var catElem) && catElem.ValueKind == JsonValueKind.String)
                    {
                        var cat = catElem.GetString();
                        if (!string.IsNullOrWhiteSpace(cat) && !string.Equals(cat, "null", StringComparison.OrdinalIgnoreCase))
                            suggestedCategory = cat.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Initial Ollama analysis failed");
            }

            // 5. Try to map address → building
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
                    bool okStreet = string.IsNullOrWhiteSpace(street) || string.Equals(b.StreetName?.Trim(), street.Trim(), StringComparison.OrdinalIgnoreCase);
                    bool okZip = string.IsNullOrWhiteSpace(zip) || string.Equals(b.PostalCode?.Trim(), zip.Trim(), StringComparison.OrdinalIgnoreCase);
                    bool okHouse = string.IsNullOrWhiteSpace(house) || string.Equals(b.HouseNumber?.Trim(), house.Trim(), StringComparison.OrdinalIgnoreCase);
                    bool okCity = string.IsNullOrWhiteSpace(city) || string.Equals(b.City?.Trim(), city.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (okStreet && okZip && okHouse && okCity)
                    {
                        matchedBuilding = b;
                        break;
                    }
                }
            }

            // 6. Validate suggested category exists
            var allCategories = ReadCategories();
            var categoryMatch = allCategories.FirstOrDefault(c =>
                string.Equals(c.Name?.Trim(), suggestedCategory, StringComparison.OrdinalIgnoreCase));
            string? suggestedCategoryName = categoryMatch?.Name;
            if (categoryMatch == null) suggestedCategoryName = null;

            // 7. Save document WITHOUT key information (will be extracted after user confirmation)
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
                KeyInformation = null, // Will be extracted after user confirmation
                UploadedAt = DateTime.UtcNow,
                UploadedBy = null,
                OrganizationId = GetCurrentUserOrganizationId(),
                BuildingId = matchedBuilding?.BuildingId,
                CategoryName = null, // Will be set after user confirmation
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Store the extracted text for later use in key information extraction
            var extractedTextPath = Path.Combine(textOutputDir, $"extracted_text_{document.DocumentId}.txt");
            await System.IO.File.WriteAllTextAsync(extractedTextPath, textForOllama).ConfigureAwait(false);

            // Save initial analysis results
            if (parsedAddress != null && parsedAddress.Count > 0)
            {
                var addressJsonPath = Path.Combine(textOutputDir, "suggested_address.json");
                var addressJson = JsonSerializer.Serialize(parsedAddress, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(addressJsonPath, addressJson).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(suggestedCategory))
            {
                var categoryTxtPath = Path.Combine(textOutputDir, "suggested_category.txt");
                await System.IO.File.WriteAllTextAsync(categoryTxtPath, suggestedCategory).ConfigureAwait(false);
            }

            // 8. Response with suggestions (user will confirm these in frontend)
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
                                            { "street",       "Couldn't identify" },
                                            { "house_number", "Couldn't identify" },
                                            { "zip_code",     "Couldn't identify" },
                                            { "city",         "Couldn't identify" }
                                        },
                SuggestedBuildingId = matchedBuilding?.BuildingId,
                SuggestedBuildingName = matchedBuilding?.Name,
                SuggestedCategoryName = suggestedCategoryName,
            });
        }

        // NEW: Endpoint to extract key information after user confirmation
        [HttpPost("{id}/extract-key-information")]
        public async Task<IActionResult> ExtractKeyInformation(int id, [FromBody] ExtractKeyInformationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("CategoryName is required");

            var orgId = GetCurrentUserOrganizationId();
            var document = _context.Documents
                .FirstOrDefault(d => d.DocumentId == id && d.OrganizationId == orgId);

            if (document == null)
                return NotFound();

            try
            {
                // Read the previously extracted text
                var textOutputDir = "/app/documents2";
                var extractedTextPath = Path.Combine(textOutputDir, $"extracted_text_{document.DocumentId}.txt");

                if (!System.IO.File.Exists(extractedTextPath))
                {
                    _logger.LogWarning("Extracted text file not found for document {DocumentId}, re-extracting", document.DocumentId);

                    // Re-extract text if file doesn't exist
                    var filePath = Path.Combine("/app/documents", document.FileName);
                    if (!System.IO.File.Exists(filePath))
                        return BadRequest("Original document file not found");

                    var fileBytes = System.IO.File.ReadAllBytes(filePath);
                    var textForOllama = await _tikaService.ExtractTextAsync(fileBytes, document.FileName).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(textForOllama) || textForOllama.Length < 50)
                    {
                        textForOllama = await _tikaService.ExtractTextAsync(fileBytes, document.FileName, true).ConfigureAwait(false);
                    }

                    textForOllama = ExtractVisibleText(textForOllama);
                    await System.IO.File.WriteAllTextAsync(extractedTextPath, textForOllama).ConfigureAwait(false);
                }

                var extractedText = await System.IO.File.ReadAllTextAsync(extractedTextPath).ConfigureAwait(false);
                // var shortText = extractedText.Length > 4_000 ? extractedText[..4_000] : extractedText;
                var shortText = extractedText;

                // Read categories directly from file as raw JSON to avoid double serialization
                var categoriesSchemaJson = System.IO.File.ReadAllText(CategoriesJsonPath);

                // Debug log the categories schema structure
                _logger.LogInformation("Categories schema for key extraction: {Schema}", categoriesSchemaJson);
                _logger.LogInformation("Requested category name: {CategoryName}", request.CategoryName);

                var keyInfoPrompt = BuildKeyInformationPrompt(shortText, categoriesSchemaJson, request.CategoryName);

                // Extract key information using second Ollama call
                Dictionary<string, string?> keyInformation = new();

                var ollamaRawResponse = await _ollamaService.GenerateAsync(keyInfoPrompt).ConfigureAwait(false);
                _logger.LogInformation("OLLAMA key information response:\n{0}", ollamaRawResponse);

                var ollamaJsonDoc = JsonDocument.Parse(ollamaRawResponse);
                var ollamaRoot = ollamaJsonDoc.RootElement;

                if (ollamaRoot.TryGetProperty("response", out var responseElem))
                {
                    var innerResponseString = responseElem.GetString();

                    var cleanedJson = innerResponseString
                        .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();

                    int first = cleanedJson.IndexOf('{');
                    int last = cleanedJson.LastIndexOf('}');
                    if (first >= 0 && last > first)
                        cleanedJson = cleanedJson[first..(last + 1)];

                    _logger.LogInformation("🧼 Cleaned Key Info JSON: {Cleaned}", cleanedJson);

                    // Defensive: Check if cleanedJson is valid JSON object before parsing
                    if (string.IsNullOrWhiteSpace(cleanedJson) || !cleanedJson.Trim().StartsWith("{"))
                    {
                        _logger.LogError("❌ Invalid key info JSON received from AI: {Cleaned}", cleanedJson);
                        return StatusCode(500, new { error = "AI key extraction returned invalid JSON.", raw = cleanedJson });
                    }

                    var cleanedJsonPath = Path.Combine(textOutputDir, "key_info_ollama_response.json");
                    using (var cleanedStream = new FileStream(cleanedJsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    using (var cleanedWriter = new StreamWriter(cleanedStream))
                    {
                        await cleanedWriter.WriteAsync(cleanedJson).ConfigureAwait(false);
                    }

                    var root = JsonDocument.Parse(cleanedJson).RootElement;

                    // Extract key information
                    if (root.TryGetProperty("key_information", out var kiObj) && kiObj.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in kiObj.EnumerateObject())
                        {
                            string value = property.Value.ValueKind switch
                            {
                                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                                JsonValueKind.Number => property.Value.GetRawText(),
                                JsonValueKind.True => "true",
                                JsonValueKind.False => "false",
                                JsonValueKind.Null => null,
                                _ => property.Value.ToString()
                            };
                            keyInformation[property.Name] = value;
                        }
                    }
                }

                // Update document with confirmed category and extracted key information
                document.CategoryName = request.CategoryName;
                document.KeyInformation = keyInformation.Count > 0 ? JsonDocument.Parse(JsonSerializer.Serialize(keyInformation)) : null;
                document.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync().ConfigureAwait(false);

                // Save extracted key information to file
                if (keyInformation.Count > 0)
                {
                    var keyInfoJsonPath = Path.Combine(textOutputDir, "final_key_information.json");
                    var keyInfoJson = JsonSerializer.Serialize(keyInformation, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(keyInfoJsonPath, keyInfoJson).ConfigureAwait(false);
                }

                _logger.LogInformation("✅ Key information extracted for document {DocumentId} with category {Category}", document.DocumentId, request.CategoryName);

                return Ok(new
                {
                    Success = true,
                    KeyInformation = keyInformation,
                    CategoryName = document.CategoryName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Key information extraction failed for document {DocumentId}", document.DocumentId);
                return StatusCode(500, new { error = "Key information extraction failed", details = ex.Message });
            }
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

            var dtos = documents.Select(document => new BitAndBeam.Dto.DocumentDto
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
                OrganizationId = document.OrganizationId,
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

            var parsedAddress = new Dictionary<string, string>{
                { "street",       "Couldn't identify" },
                { "house_number", "Couldn't identify" },
                { "zip_code",     "Couldn't identify" },
                { "city",         "Couldn't identify" }
            };

            var dto = new BitAndBeam.Dto.DocumentDto
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

            try
            {
                // Read raw file content for debugging
                var rawContent = System.IO.File.ReadAllText(CategoriesJsonPath);
                _logger.LogInformation("📋 Raw categories file content: {Content}", rawContent);

                var categories = ReadCategories();
                _logger.LogInformation("🔍 Parsed {Count} categories", categories.Count);

                var result = categories.Select(c => new
                {
                    name = c.Name,
                    description = c.Description,
                    fields = c.Fields,
                }).ToList();

                // Also return debugging info
                return Ok(new
                {
                    categories = result,
                    debug = new
                    {
                        rawFileExists = System.IO.File.Exists(CategoriesJsonPath),
                        rawFileLength = rawContent.Length,
                        parsedCategoriesCount = categories.Count,
                        serializedSchema = JsonSerializer.Serialize(categories)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reading categories: {Error}", ex.Message);
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

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

            var dto = new BitAndBeam.Dto.DocumentDto
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
        public async Task<IActionResult> UpdateDocumentMetadata(int id, [FromBody] DocumentMetadataPatchRequest request)
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

            var previousCategoryName = document.CategoryName;

            // Handle CategoryName logic
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

            // Handle KeyInformation - but only if provided explicitly
            if (request.KeyInformation != null)
                document.KeyInformation = JsonDocument.Parse(JsonSerializer.Serialize(request.KeyInformation));

            // If category changed, trigger key information re-extraction
            bool categoryChanged = !string.Equals(previousCategoryName, document.CategoryName, StringComparison.OrdinalIgnoreCase);

            _context.SaveChanges();

            // Re-extract key information if category changed and new category is not null
            if (categoryChanged && !string.IsNullOrWhiteSpace(document.CategoryName))
            {
                try
                {
                    _logger.LogInformation("Category changed for document {DocumentId}, re-extracting key information", document.DocumentId);

                    // Call the key information extraction method
                    var extractRequest = new ExtractKeyInformationRequest { CategoryName = document.CategoryName };
                    var extractResult = await ExtractKeyInformationForDocument(document.DocumentId, extractRequest).ConfigureAwait(false);

                    if (extractResult != null)
                    {
                        // Reload the document to get updated key information
                        document = _context.Documents.FirstOrDefault(d => d.DocumentId == id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to re-extract key information for document {DocumentId} after category change", document.DocumentId);
                    // Continue with the response even if key information extraction fails
                }
            }

            var dto = new BitAndBeam.Dto.DocumentDto
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

        // Helper method for key information extraction (used by both endpoints)
        private async Task<Dictionary<string, string?>?> ExtractKeyInformationForDocument(int documentId, ExtractKeyInformationRequest request)
        {
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == documentId);
            if (document == null) return null;

            try
            {
                // Read the previously extracted text
                var textOutputDir = "/app/documents2";
                var extractedTextPath = Path.Combine(textOutputDir, $"extracted_text_{document.DocumentId}.txt");

                if (!System.IO.File.Exists(extractedTextPath))
                {
                    // Re-extract text if file doesn't exist
                    var filePath = Path.Combine("/app/documents", document.FileName);
                    if (!System.IO.File.Exists(filePath)) return null;

                    var fileBytes = System.IO.File.ReadAllBytes(filePath);
                    var textForOllama = await _tikaService.ExtractTextAsync(fileBytes, document.FileName).ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(textForOllama) || textForOllama.Length < 50)
                    {
                        textForOllama = await _tikaService.ExtractTextAsync(fileBytes, document.FileName, true).ConfigureAwait(false);
                    }

                    textForOllama = ExtractVisibleText(textForOllama);
                    await System.IO.File.WriteAllTextAsync(extractedTextPath, textForOllama).ConfigureAwait(false);
                }

                var extractedText = await System.IO.File.ReadAllTextAsync(extractedTextPath).ConfigureAwait(false);
                // var shortText = extractedText.Length > 4_000 ? extractedText[..4_000] : extractedText;
                var shortText = extractedText;

                // Build category-specific prompt for key information extraction
                // Read categories directly from file as raw JSON to avoid double serialization
                var categoriesSchemaJson = System.IO.File.ReadAllText(CategoriesJsonPath);
                var keyInfoPrompt = BuildKeyInformationPrompt(shortText, categoriesSchemaJson, request.CategoryName);

                // Extract key information using Ollama call
                Dictionary<string, string?> keyInformation = new();

                var ollamaRawResponse = await _ollamaService.GenerateAsync(keyInfoPrompt).ConfigureAwait(false);

                var ollamaJsonDoc = JsonDocument.Parse(ollamaRawResponse);
                var ollamaRoot = ollamaJsonDoc.RootElement;

                if (ollamaRoot.TryGetProperty("response", out var responseElem))
                {
                    var innerResponseString = responseElem.GetString();

                    var cleanedJson = innerResponseString
                        .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();

                    int first = cleanedJson.IndexOf('{');
                    int last = cleanedJson.LastIndexOf('}');
                    if (first >= 0 && last > first)
                        cleanedJson = cleanedJson[first..(last + 1)];

                    var root = JsonDocument.Parse(cleanedJson).RootElement;

                    // Extract key information
                    if (root.TryGetProperty("key_information", out var kiObj) && kiObj.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in kiObj.EnumerateObject())
                        {
                            string value = property.Value.ValueKind switch
                            {
                                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                                JsonValueKind.Number => property.Value.GetRawText(),
                                JsonValueKind.True => "true",
                                JsonValueKind.False => "false",
                                JsonValueKind.Null => null,
                                _ => property.Value.ToString()
                            };
                            keyInformation[property.Name] = value;
                        }
                    }
                }

                // Update document with extracted key information
                document.KeyInformation = keyInformation.Count > 0 ? JsonDocument.Parse(JsonSerializer.Serialize(keyInformation)) : null;
                document.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync().ConfigureAwait(false);

                return keyInformation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Key information extraction failed for document {DocumentId}", documentId);
                return null;
            }
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
            }
            else
            {
            }

            // Also delete extracted text file
            var textOutputDir = "/app/documents2";
            var extractedTextPath = Path.Combine(textOutputDir, $"extracted_text_{document.DocumentId}.txt");
            if (System.IO.File.Exists(extractedTextPath))
            {
                System.IO.File.Delete(extractedTextPath);
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

        // Request/Response Models
        public class DocumentMetadataPatchRequest
        {
            public string? CategoryName { get; set; }
            public int? BuildingId { get; set; }
            public Dictionary<string, string?>? KeyInformation { get; set; }
        }

        public class DocumentUpdateRequest
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
        }

        public class ExtractKeyInformationRequest
        {
            public string CategoryName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Request model for document chatbot queries
        /// </summary>
        public class DocumentChatbotRequest
        {
            /// <summary>
            /// User's input/question to ask about the document
            /// </summary>
            public string UserInput { get; set; }
        }

        /// <summary>
        /// Response model for document chatbot queries
        /// </summary>
        public class DocumentChatbotResponse
        {
            /// <summary>
            /// The response from the chatbot
            /// </summary>
            public string Response { get; set; }
        }

        /// <summary>
        /// Query the chatbot about a specific document
        /// </summary>
        /// <param name="documentId">The ID of the document to query</param>
        /// <param name="request">The user's input/question</param>
        /// <param name="httpClientFactory">HTTP client factory for Ollama service</param>
        /// <returns>The chatbot's response</returns>
        [HttpPost("{documentId}/ask")]
        [ProducesResponseType(typeof(DocumentChatbotResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AskDocumentChatbot(int documentId, [FromBody] DocumentChatbotRequest request, [FromServices] IHttpClientFactory httpClientFactory)
        {
            // Validate the request
            if (request == null || string.IsNullOrWhiteSpace(request.UserInput))
            {
                return BadRequest(new { error = "User input is required." });
            }

            // Get the current user's organization ID
            var orgId = GetCurrentUserOrganizationId();

            try
            {
                // Get the document from the database
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId && d.OrganizationId == orgId).ConfigureAwait(false);

                // Check if the document exists
                if (document == null)
                {
                    return NotFound(new { error = $"Document with ID {documentId} not found." });
                }

                // Get the document content
                var uploadsPath = Path.Combine("/app/documents");

                // Check if directory exists and log results
                bool directoryExists = Directory.Exists(uploadsPath);
                _logger.LogInformation("📂 Documents directory exists: {DirectoryExists}, path: {UploadsPath}", directoryExists, uploadsPath);

                // Check if we have the FilePath from the database vs using filename
                _logger.LogInformation("📃 Document record info - FilePath: {FilePath}, FileName: {FileName}", document.FilePath, document.FileName);

                // Try both potential paths
                var fullPathUsingFileName = Path.Combine(uploadsPath, document.FileName);
                var fullPathUsingFilePath = document.FilePath; // Use the stored FilePath directly

                _logger.LogInformation("📄 Checking file at paths:\n1) {Path1}\n2) {Path2}", fullPathUsingFileName, fullPathUsingFilePath);

                var fullPath = "";

                // Check which path exists
                if (System.IO.File.Exists(fullPathUsingFileName))
                {
                    _logger.LogInformation("✅ File found using FileName at: {Path}", fullPathUsingFileName);
                    fullPath = fullPathUsingFileName;
                }
                else if (System.IO.File.Exists(fullPathUsingFilePath))
                {
                    _logger.LogInformation("✅ File found using FilePath at: {Path}", fullPathUsingFilePath);
                    fullPath = fullPathUsingFilePath;
                }
                else
                {
                    // Check parent directory contents to debug
                    if (directoryExists)
                    {
                        var files = Directory.GetFiles(uploadsPath);
                        _logger.LogInformation("📁 Files in upload directory: {FileCount}", files.Length);
                        foreach (var file in files.Take(10)) // List up to 10 files
                        {
                            _logger.LogInformation("📄 Found file: {FileName}", Path.GetFileName(file));
                        }
                    }

                    return NotFound(new { error = $"Document file not found on server. Checked paths:\n{fullPathUsingFileName}\n{fullPathUsingFilePath}" });
                }

                // Extract document content from file
                string documentContent;
                try
                {
                    _logger.LogInformation("📄 Attempting to read file at path: {FilePath} for document {DocumentId}", fullPath, documentId);

                    // Check if file exists before attempting to read
                    if (!System.IO.File.Exists(fullPath))
                    {
                        _logger.LogError("❌ File not found at path: {FilePath} for document {DocumentId}", fullPath, documentId);
                        return BadRequest(new { error = $"Document file not found at {fullPath}." });
                    }

                    try
                    {
                        byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
                        _logger.LogInformation("📊 Successfully read {ByteCount} bytes from file for document {DocumentId}", fileBytes.Length, documentId);

                        // Document file read successfully, now attempt extraction
                        documentContent = await _tikaService.ExtractTextAsync(fileBytes, document.FileName).ConfigureAwait(false);
                        _logger.LogInformation("📝 Text extraction completed for document {DocumentId}, extracted {CharCount} characters", documentId, documentContent?.Length ?? 0);

                        // If the extracted text is very short (which may happen with scanned PDFs), try OCR extraction
                        if (string.IsNullOrWhiteSpace(documentContent) || documentContent.Length < 100)
                        {
                            _logger.LogInformation("⚠️ Initial text extraction returned minimal content, trying OCR for document {DocumentId}", documentId);
                            documentContent = await _tikaService.ExtractTextAsync(fileBytes, document.FileName, true).ConfigureAwait(false);
                            _logger.LogInformation("📝 OCR extraction completed for document {DocumentId}, extracted {CharCount} characters", documentId, documentContent?.Length ?? 0);
                        }
                    }
                    catch (IOException ioEx)
                    {
                        _logger.LogError(ioEx, "❌ IO error reading file: {FilePath} for document {DocumentId}", fullPath, documentId);
                        return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"Error reading document file: {ioEx.Message}" });
                    }

                    if (string.IsNullOrWhiteSpace(documentContent))
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to extract text from document." });
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to extract text from document {DocumentId}", documentId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to extract text from document." });
                }

                documentContent = ExtractVisibleText(documentContent);
                // Truncate document content if too long
                var maxContentLength = 7000; // Adjust based on model context window
                // var truncatedContent = documentContent.Length > maxContentLength
                //     ? documentContent.Substring(0, maxContentLength)
                //     : documentContent;
                var truncatedContent = documentContent;

                // Construct the prompt for Ollama
                var prompt = $$"""
                You are a helpful assistant answering questions about contents of document.
                Use ONLY the information from the **DOCUMENT CONTENT** provided below, to provide a concise and accurate answer to the **USER QUESTION**.
                If the answer cannot be found in the document, say so clearly - do not create new information.
                
                **DOCUMENT CONTENT**:
                {{truncatedContent}}

                **USER QUESTION**:
                {{request.UserInput}}

                """;

                try
                {
                    // Send the request to Ollama
                    var jsonResponse = await _ollamaService.GenerateAsync(prompt).ConfigureAwait(false);

                    // Parse the response
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaController.OllamaResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Return the response
                    return Ok(new DocumentChatbotResponse
                    {
                        Response = ollamaResponse?.Response
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error communicating with Ollama service for document {DocumentId}", documentId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Error communicating with Ollama service." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error in AskDocumentChatbot for document {DocumentId}: {ErrorMessage}", documentId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        // PROMPT METHODS

        /// <summary>
        /// Builds the initial prompt for address and category suggestion only
        /// </summary>
        private string BuildInitialPrompt(string extractedText, string categoriesSchemaJson)
        {
            return $$"""
            You are an intelligent document analyzer for German documents.

            Given the **extracted text** from a German document, your task is to analyze and extract the following information in a strict JSON format:

            Your answer MUST include the following top-level fields: "address" and "category".

            **Example Format**:
            {
                "address": {
                    "street": "<string|null>",
                    "house_number": "<string|null>",
                    "zip_code": "<string|null>",
                    "city": "<string|null>"
                },
                "category": "<string|null>"
            }

            **TASK A** → Extract an **address** if present.
            Look for labels like:
            "Adresse", "Anschrift", "Standort", "Objektadresse", "Gebäudeadresse", "Hausanschrift", "Liegenschaft", "Baustellenadresse", "Postanschrift", "Immobilienadresse",
            or field names such as "Straße", "Haus-Nr.", "PLZ", "Ort", and the same terms in free text.

            **TASK B** → Choose the SINGLE best-matching **category** from "categories_schema" (use null if none fits)

            **Rules**
            • Every value must be a JSON string or null — no units, no comments.
            • Output MUST be valid JSON that parses with 'JSON.parse()'.
            • If any field cannot be detected, output it with a null value.
            • Do **not** wrap the answer in markdown or code fences.
            • Do NOT extract key information fields - only address and category.

            **categories_schema**:
            {{categoriesSchemaJson}}

            **Extracted Text**:
            {{extractedText}}
            """;
        }

        /// <summary>
        /// Builds the prompt for extracting key information for a specific category
        /// </summary>
        private string BuildKeyInformationPrompt(string extractedText, string categoriesSchemaJson, string categoryName)
        {
            // Parse the categoriesSchemaJson to extract the fields for the given categoryName
            using var doc = JsonDocument.Parse(categoriesSchemaJson);
            var root = doc.RootElement;
            var categories = root.GetProperty("categories");
            JsonElement? category = null;
            foreach (var cat in categories.EnumerateArray())
            {
                if (cat.TryGetProperty("name", out var nameProp) && nameProp.GetString() == categoryName)
                {
                    category = cat;
                    break;
                }
            }
            if (category == null)
            {
                throw new ArgumentException($"Category '{categoryName}' not found in categories schema.");
            }

            // Extract the fields array for the category
            var fields = category.Value.GetProperty("fields");
            // Build a list of field names for the prompt example
            var fieldNames = fields.EnumerateArray().Select(f => f.GetProperty("name").GetString()).ToList();
            // Build the example JSON for key_information
            var keyInfoExample = string.Join(",\n        ", fieldNames.Select(fn => $"\"{fn}\": \"<string|null>\""));

            // Compose the prompt
            return $$"""
            You are an intelligent document analyzer specialized in extracting key information from German documents.

            Given the **extracted text** from a German document and the specific **category schema** for "{{categoryName}}", your task is to extract the key information fields in a strict JSON format.

            Your answer MUST include the following top-level field: "key_information".

            **Example Format**:
            {
                "key_information": {
                    {{keyInfoExample}}
                }
            }

            **TASK** → For the category "{{categoryName}}", extract the **key information** fields defined in the category schema and return them under "key_information".
            For every field in the category's 'fields' array:
            • Use the field's **name** as the JSON key.
            • Try to extract the corresponding value from the document; if not found, set it to null.
            • Only include the fields declared for this category — no extra keys.
            • Look for field values in various formats: labels, tables, forms, free text.

            **Rules**
            • Every value must be a JSON string or null — no units, no comments.
            • Output MUST be valid JSON that parses with 'JSON.parse()'.
            • If any field cannot be detected, output it with a null value.
            • Do **not** wrap the answer in markdown or code fences.
            • Be thorough in searching for field values throughout the entire document text.

            **category_schema for "{{categoryName}}"**:
            {{category.Value}}

            **Extracted Text**:
            {{extractedText}}
            """;
        }

        private string ExtractVisibleText(string tikaHtml)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(tikaHtml);

            // Remove unwanted elements
            var unwantedTags = new[] { "script", "style", "head", "meta", "link" };
            foreach (var tag in unwantedTags)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                        node.Remove();
                }
            }

            // Extract visible text from <body>
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode == null)
                return tikaHtml.Trim(); // fallback

            var textNodes = bodyNode.Descendants()
                .Where(n => n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
                .Select(n => HtmlEntity.DeEntitize(n.InnerText.Trim()));

            // Join all text nodes with a space
            var rawText = string.Join(" ", textNodes);

            // Replace all \n with a space
            rawText = rawText.Replace("\n", " ");

            // Collapse multiple spaces/tabs into a single space
            var cleanedText = Regex.Replace(rawText, @"[ \t]+", " ");

            return cleanedText.Trim();
        }
    }
}
