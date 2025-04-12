using HR_KD.Data;
using HR_KD.DTOs;
using HR_KD.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HR_KD.Controllers
{
    public class EmployeeImportController : Controller
    {
        private readonly ExcelTemplateService _excelService;
        private readonly HrDbContext _context;

        public EmployeeImportController(HrDbContext context, ExcelTemplateService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        public IActionResult Index()
        {
            return View(); 
        }

        public IActionResult DownloadTemplate()
        {
            var fileBytes = _excelService.GenerateTemplateWithData();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Import_NhanVien.xlsx");
        }
        #region Xem trước dữ liệu tải lên 
        public IActionResult Review()
        {
            var json = HttpContext.Session.GetString("ImportedEmployees");
            var errJson = HttpContext.Session.GetString("ImportErrors");

            if (string.IsNullOrEmpty(json)) return RedirectToAction("Index");

            var employees = JsonConvert.DeserializeObject<List<ImportNhanVienDto>>(json);
            var errors = JsonConvert.DeserializeObject<Dictionary<int, string>>(errJson ?? "{}");

            ViewBag.Errors = errors;
            ViewBag.ChucVus = _context.ChucVus.ToDictionary(c => c.MaChucVu, c => c.TenChucVu);
            ViewBag.PhongBans = _context.PhongBans.ToDictionary(p => p.MaPhongBan, p => p.TenPhongBan);

            return View(employees);
        }
        #endregion

        #region tải lên excel
        [HttpPost("Upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction("Index");

            using var stream = file.OpenReadStream();
            var result = await _excelService.ParseExcelWithValidation(stream);

            // Gắn lỗi vào từng dòng (dễ render màu dòng)
            foreach (var emp in result.Employees)
            {
                if (result.Errors.TryGetValue(emp.RowIndex, out var error))
                    emp.RowError = error;
            }

            return View("Review", result.Employees);
        }
        #endregion



    }
}
