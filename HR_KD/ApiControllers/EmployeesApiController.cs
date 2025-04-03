using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.DTOs;
using System.IO;
using System.Collections.Generic;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<EmployeesApiController> _logger;

        public EmployeesApiController(HrDbContext context, EmailService emailService, ILogger<EmployeesApiController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        #region Lấy danh sách nhân viên
        [HttpGet]
        public IActionResult GetEmployees()
        {
            var employees = _context.NhanViens
                .Include(e => e.MaPhongBanNavigation)
                .Include(e => e.MaChucVuNavigation)
                .Select(e => new
                {
                    e.MaNv,
                    e.HoTen,
                    e.NgaySinh,
                    GioiTinh = e.GioiTinh.HasValue ? (e.GioiTinh.Value ? "Nam" : "Nữ") : "Không xác định",
                    e.DiaChi,
                    e.Sdt,
                    e.Email,
                    e.TrinhDoHocVan,
                    e.NgayVaoLam,
                    ChucVu = e.MaChucVuNavigation.TenChucVu,
                    PhongBan = e.MaPhongBanNavigation.TenPhongBan,
                    e.AvatarUrl
                })
                .ToList();
            return Ok(employees);
        }
        #endregion

        #region Thêm nhân viên 
        [HttpPost("CreateEmployee")]
        public IActionResult CreateEmployee([FromForm] CreateEmployeeDTO employeeDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = string.Join(" | ", errors) });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // ✅ Kiểm tra số điện thoại đã tồn tại chưa
                if (_context.TaiKhoans.Any(t => t.Username == employeeDto.Sdt))
                {
                    return Conflict(new { message = "Số điện thoại đã được sử dụng." });
                }

                // ✅ Kiểm tra phòng ban & chức vụ hợp lệ
                if (!_context.PhongBans.Any(p => p.MaPhongBan == employeeDto.MaPhongBan))
                {
                    return BadRequest(new { message = "Phòng ban không tồn tại." });
                }

                if (!_context.ChucVus.Any(c => c.MaChucVu == employeeDto.MaChucVu))
                {
                    return BadRequest(new { message = "Chức vụ không tồn tại." });
                }

                // ✅ Tạo nhân viên mới
                var employee = new NhanVien
                {
                    HoTen = employeeDto.HoTen,
                    NgaySinh = DateOnly.FromDateTime(employeeDto.NgaySinh),
                    GioiTinh = employeeDto.GioiTinh,
                    DiaChi = employeeDto.DiaChi,
                    Sdt = employeeDto.Sdt,
                    Email = employeeDto.Email,
                    TrinhDoHocVan = employeeDto.TrinhDoHocVan,
                    MaPhongBan = employeeDto.MaPhongBan,
                    MaChucVu = employeeDto.MaChucVu,
                    NgayVaoLam = DateOnly.FromDateTime(employeeDto.NgayVaoLam),
                    AvatarUrl = null
                };

                _context.NhanViens.Add(employee);
                _context.SaveChanges();
                int maNvMoi = employee.MaNv;

                // ✅ Gán quyền hạn mặc định
                var defaultRoles = new List<string> { "EMPLOYEE" };
                var validRoles = _context.QuyenHans
                    .Where(q => defaultRoles.Contains(q.MaQuyenHan))
                    .Select(q => q.MaQuyenHan)
                    .ToList();

                if (!validRoles.Any())
                {
                    _logger.LogWarning("Không có quyền hạn hợp lệ để gán cho nhân viên mới.");
                    return BadRequest(new { message = "Không có quyền hạn hợp lệ để gán cho nhân viên mới." });
                }

                // ✅ Tạo tài khoản
                string defaultPassword = "123456";
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                var taiKhoan = new TaiKhoan
                {
                    Username = employeeDto.Sdt,
                    PasswordHash = hashedPassword,
                    MaNv = maNvMoi
                };
                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();
                //int maTaiKhoanMoi = taiKhoan.MaTaiKhoan; // ✅ Lấy MaTaiKhoan để gán quyền

                // ✅ Gán quyền hạn cho tài khoản
                foreach (var role in validRoles)
                {
                    _context.TaiKhoanQuyenHans.Add(new TaiKhoanQuyenHan
                    {
                        Username = taiKhoan.Username, // ✅ Fix lỗi: dùng MaTaiKhoan thay vì Username
                        MaQuyenHan = role
                    });
                }
                _context.SaveChanges();

                // ✅ Xử lý ảnh đại diện
                if (employeeDto.AvatarUrl != null)
                {
                    try
                    {
                        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        var fileName = $"{maNvMoi}_{Path.GetFileName(employeeDto.AvatarUrl.FileName)}";
                        var filePath = Path.Combine(basePath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            employeeDto.AvatarUrl.CopyTo(stream);
                        }

                        employee.AvatarUrl = $"/avatars/{fileName}";
                        _context.NhanViens.Update(employee);
                        _context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi xử lý ảnh đại diện.");
                    }
                }

                // ✅ Gửi email thông báo
                try
                {
                    string subject = "Thông tin tài khoản nhân viên";
                    string body = $@"
                        <h3>Chào {employeeDto.HoTen},</h3>
                        <p>Bạn đã được tạo tài khoản nhân viên.</p>
                        <p><strong>Tài khoản:</strong> {taiKhoan.Username}</p>
                        <p><strong>Mật khẩu:</strong> {defaultPassword}</p>
                        <p>Vui lòng đăng nhập và đổi mật khẩu ngay.</p>";

                    _emailService.SendEmail(employeeDto.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email.");
                }

                transaction.Commit();

                return CreatedAtAction(nameof(GetEmployees), new { id = maNvMoi }, employee);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Lỗi khi tạo nhân viên.");
                return StatusCode(500, new { message = "Lỗi server. Xem log để biết chi tiết.", error = ex.Message });
            }
        }
        #endregion


    }
}
