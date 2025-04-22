using HR_KD.Data;
using HR_KD.DTOs;
using HR_KD.Helpers;
using HR_KD.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HR_KD.Controllers
{
    public class EmployeeImportController : Controller
    {
        private readonly HrDbContext _context;
        private readonly ExcelTemplateService _excelService;
        private readonly UsernameGeneratorService _usernameGen;

        public EmployeeImportController(
            HrDbContext context,
            ExcelTemplateService excelService,
            UsernameGeneratorService usernameGen)
        {
            _context = context;
            _excelService = excelService;
            _usernameGen = usernameGen;
        }

        public IActionResult Index() => View();

        public IActionResult DownloadTemplate()
        {
            var fileBytes = _excelService.GenerateTemplateWithData();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Template_Import_NhanVien.xlsx");
        }

        #region Upload + Review dữ liệu từ Excel
        [HttpPost("Upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction("Index");

            using var stream = file.OpenReadStream();
            var result = await _excelService.ParseExcelWithValidation(stream);

            // Gắn lỗi vào từng dòng (dễ render màu)
            foreach (var emp in result.Employees)
            {
                emp.RowIndex = result.Employees.IndexOf(emp) + 2; // để log lại dòng excel nếu cần
                if (result.Errors.TryGetValue(emp.RowIndex, out var error))
                    emp.RowError = error;
            }

            HttpContext.Session.SetString("ImportedEmployees", JsonConvert.SerializeObject(result.Employees));
            HttpContext.Session.SetString("ImportErrors", JsonConvert.SerializeObject(result.Errors));

            return RedirectToAction("Review");
        }
        #endregion

        #region Xem trước dữ liệu
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

        #region Lưu hàng loạt
        [HttpPost("/api/EmployeesApi/SaveImportBatch")]
        public IActionResult SaveImportBatch([FromForm] List<ImportNhanVienDto> list)
        {
            var savedCount = 0;

            foreach (var dto in list)
            {
                try
                {
                    // Skip nếu lỗi
                    if (!string.IsNullOrEmpty(dto.RowError)) continue;

                    // Kiểm tra trùng
                    if (_context.TaiKhoans.Any(x => x.Username == dto.Sdt)) continue;
                    if (_context.NhanViens.Any(x => x.Email == dto.Email)) continue;

                    var nv = new NhanVien
                    {
                        HoTen = dto.HoTen,
                        NgaySinh = DateOnly.FromDateTime(dto.NgaySinh),
                        GioiTinh = dto.GioiTinh,
                        DiaChi = "", // Tùy nhập sau
                        Sdt = dto.Sdt,
                        Email = dto.Email,
                        TrinhDoHocVan = dto.TrinhDoHocVan,
                        MaPhongBan = dto.MaPhongBan ?? 0,
                        MaChucVu = dto.MaChucVu ?? 0,
                        NgayVaoLam = DateOnly.FromDateTime(dto.NgayVaoLam),
                        AvatarUrl = null
                    };

                    _context.NhanViens.Add(nv);
                    _context.SaveChanges();

                    // Tạo tài khoản
                    string username = _usernameGen.GenerateUsername(dto.HoTen, nv.MaNv);
                    string pass = "123456";
                    string salt = PasswordHelper.GenerateRandomKey();
                    string hash = BCrypt.Net.BCrypt.HashPassword(pass + salt);

                    var acc = new TaiKhoan
                    {
                        Username = username,
                        PasswordHash = hash,
                        PasswordSalt = salt,
                        MaNv = nv.MaNv
                    };

                    _context.TaiKhoans.Add(acc);

                    _context.TaiKhoanQuyenHans.Add(new TaiKhoanQuyenHan
                    {
                        Username = username,
                        MaQuyenHan = "EMPLOYEE"
                    });

                    _context.SaveChanges();
                    savedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi dòng {dto.RowIndex}: {ex.Message}");
                }
            }

            return Json(new { success = true, message = $"Đã lưu {savedCount} nhân viên thành công." });
        }
        #endregion
    }
}
