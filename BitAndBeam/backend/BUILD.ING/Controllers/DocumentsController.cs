using Microsoft.AspNetCore.Mvc;
using Build.ING.Data;
using Build.ING.Models;


namespace Build.ING.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentsController(AppDbContext context, IWebHostEnvironment env)
        {
            Console.WriteLine("🚀 DocumentsController loaded");
            _context = context;
            _env = env;
        }
        // Simulate current user group (hardcoded for now)
        private string GetCurrentUserGroupId()
        {
            return "group2"; // change this to test other groups later
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var uploadsPath = Path.Combine(_env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, file.FileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var document = new Document
            {
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = file.FileName,
                FilePath = filePath,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = "someUser@example.com", // for now hardcoded
                GroupId = GetCurrentUserGroupId()
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { document.Id });
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
            var document = _context.Documents.FirstOrDefault(d => d.Id == id && d.GroupId == groupId);
            if (document == null)
                return NotFound();

            return Ok(document);
        }
        [HttpPut("{id}")]
        public IActionResult UpdateDocumentTitle(int id, [FromBody] DocumentUpdateRequest request)
        {
            var document = _context.Documents.FirstOrDefault(d => d.Id == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            document.Title = request.Title;
            _context.SaveChanges();

            return Ok(document);
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteDocument(int id)
        {
            var document = _context.Documents.FirstOrDefault(d => d.Id == id && d.GroupId == GetCurrentUserGroupId());
            if (document == null)
                return NotFound();

            // Try to delete the physical file
            if (System.IO.File.Exists(document.FilePath))
            {
                System.IO.File.Delete(document.FilePath);
            }

            _context.Documents.Remove(document);
            _context.SaveChanges();

            return NoContent(); // 204 - success, no body
        }
    }
}
