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
                GroupId = "group1" //
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { document.Id });
        }
        [HttpGet]
        public IActionResult GetAllDocuments()
        {
            var documents = _context.Documents.ToList();
            return Ok(documents);
        }
        [HttpGet("{id}")]
        public IActionResult GetDocumentById(int id)
        {
            var document = _context.Documents.FirstOrDefault(d => d.Id == id);
            if (document == null)
                return NotFound();

            return Ok(document);
        }

    }
}
