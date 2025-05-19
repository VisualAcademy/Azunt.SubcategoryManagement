using Microsoft.AspNetCore.Mvc;
using Azunt.SubcategoryManagement;
using System.Threading.Tasks;
using System.IO;
using System;

namespace Azunt.Apis.Subcategories
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubcategoryFileController : ControllerBase
    {
        private readonly ISubcategoryStorageService _storageService;

        public SubcategoryFileController(ISubcategoryStorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        /// 게시글 첨부파일 다운로드
        /// GET /api/SubcategoryFile/{fileName}
        /// </summary>
        [HttpGet("{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                var stream = await _storageService.DownloadAsync(fileName);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"File not found: {fileName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Download error: {ex.Message}");
            }
        }
    }
}