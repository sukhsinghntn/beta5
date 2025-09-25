using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DynamicFormsApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FileUploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            const long maxSize = 5 * 1024 * 1024;
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            if (file.Length > maxSize)
                return BadRequest("File exceeds 5 MB limit.");

            // Generate a unique filename to prevent overwriting
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);


            // Save the file to the server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative path to the uploaded file
            var relativePath = Path.Combine("uploads", uniqueFileName);
            return Ok(new { filePath = relativePath });
        }
    }
}
