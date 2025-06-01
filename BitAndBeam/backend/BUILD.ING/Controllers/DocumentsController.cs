using BUILD.ING.Data;
using BUILD.ING.Models;
using Microsoft.AspNetCore.Mvc;
using BitAndBeam.Tika;
using System.IO;

namespace BUILD.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly TikaService _tikaService;

        public DocumentsController(AppDbContext context, IWebHostEnvironment env, TikaService tikaService)
        {
            Console.WriteLine("🚀 DocumentsController loaded");
            _context = context;
            _env = env;
            _tikaService = tikaService;
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

            // Create a memory stream to read the file content for metadata extraction
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;

            // Extract metadata using TikaService
            var fileBytes = memoryStream.ToArray();
            var metadata = await _tikaService.ExtractMetadataAsync(fileBytes, file.FileName);
            
            // Save file to disk
            using var fileStream = new FileStream(fullPath, FileMode.Create);
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);

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
                Metadata = metadata,  // Store the extracted metadata from Tika
                UploadedAt = DateTime.UtcNow,
                UploadedBy = null,
                GroupId = GetCurrentUserGroupId()
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/documents/{document.FileName}";

            return Ok(new { document.DocumentId, FileUrl = fileUrl });
        }

        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            var groupId = GetCurrentUserGroupId();
            var documents = _context.Documents.Where(d => d.GroupId == groupId).ToList();
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public IActionResult GetDocumentById(int id)
        {
            var groupId = GetCurrentUserGroupId();
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == groupId);
            if (document == null)
                return NotFound();

            return Ok(document);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateDocumentTitle(int id, [FromBody] DocumentUpdateRequest request)
        {
            var document = _context.Documents.FirstOrDefault(d => d.DocumentId == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            document.Title = request.Title;
            _context.SaveChanges();

            return Ok(document);
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
            return File(fileBytes, "application/pdf", document.FileName);
        }
    }
}
