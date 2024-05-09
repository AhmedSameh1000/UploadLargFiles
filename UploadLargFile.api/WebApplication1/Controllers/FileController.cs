using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _Host;
        private readonly AppDbContext _AppDbContext;

        public FileController(IWebHostEnvironment webHostEnvironment, AppDbContext appDbContext)
        {
            _Host = webHostEnvironment;
            _AppDbContext = appDbContext;
        }

        [HttpGet("Downlaod File")]
        public async Task<IActionResult> DownlaodFile()
        {
            var filePath = await _AppDbContext.files.FirstOrDefaultAsync();

            var filename = Path.GetFileName(filePath.Path);
            if (filePath is null)
            {
                return NotFound();
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath.Path);
            var name = Path.GetFileName(filename) + ".mp4";

            return File(fileBytes, "application/octet-stream", name);
        }
    }
}