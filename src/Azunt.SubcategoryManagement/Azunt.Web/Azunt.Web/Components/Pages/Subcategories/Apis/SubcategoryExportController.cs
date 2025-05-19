using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Azunt.SubcategoryManagement;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Azunt.Apis.Subcategories
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubcategoryExportController : ControllerBase
    {
        private readonly ISubcategoryRepository _subcategoryRepository;

        public SubcategoryExportController(ISubcategoryRepository subcategoryRepository)
        {
            _subcategoryRepository = subcategoryRepository;
        }

        /// <summary>
        /// 게시글 목록 엑셀 다운로드
        /// GET /api/SubcategoryExport/Excel
        /// </summary>
        [HttpGet("Excel")]
        public async Task<IActionResult> ExportToExcel()
        {
            var items = await _subcategoryRepository.GetAllAsync();

            if (!items.Any())
                return NotFound("No subcategory records found.");

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Subcategories");

            var range = sheet.Cells["B2"].LoadFromCollection(
                items.Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Title,
                    m.Category,
                    Created = m.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    m.CreatedBy
                }),
                PrintHeaders: true
            );

            var header = sheet.Cells["B2:G2"];
            sheet.DefaultColWidth = 20;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.WhiteSmoke);
            range.Style.Border.BorderAround(ExcelBorderStyle.Medium);

            header.Style.Font.Bold = true;
            header.Style.Font.Color.SetColor(Color.White);
            header.Style.Fill.BackgroundColor.SetColor(Color.DarkGreen);

            var content = package.GetAsByteArray();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{DateTime.Now:yyyyMMddHHmmss}_Subcategories.xlsx");
        }
    }
}