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
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var uploadsPath = Path.Combine("/app/documents");
            Directory.CreateDirectory(uploadsPath);

            var fullPath = Path.Combine(uploadsPath, file.FileName);

            // Save the file to disk
            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream).ConfigureAwait(false);

            // Extract metadata with Tika
            string metadata = "{}";
            try
            {
                // Convert file to byte array for Tika processing
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms).ConfigureAwait(false);
                    fileBytes = ms.ToArray();
                }

                // Call Tika service to extract metadata
                metadata = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName).ConfigureAwait(false);
                _logger.LogInformation("Successfully extracted metadata for file {FileName}", file.FileName);
            }
            catch (Exception ex)
            {
                // Log error but continue with upload process
                _logger.LogError(ex, "Failed to extract metadata for file {FileName}", file.FileName);
                // Empty metadata will be stored
            }

            var document = new Document
            {
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = file.FileName,
                FilePath = file.FileName, // Just store file name
                FileType = Path.GetExtension(file.FileName)?.TrimStart('.').ToLower() ?? "unknown",
                FileSize = (int) file.Length,
                UploadDate = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                Version = "1.0",
                Status = "draft",
                IsPublic = false,
                Description = "No description provided",
                Metadata = metadata, // Store the extracted metadata
                UploadedAt = DateTime.UtcNow,
                UploadedBy = null,
                GroupId = GetCurrentUserGroupId()
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/documents/{document.FileName}";

            return Ok(new { document.DocumentId, FileUrl = fileUrl, HasMetadata = metadata != "{}" });
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
    }
}
