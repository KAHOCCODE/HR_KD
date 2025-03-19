using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HR_KD.DTOs;
using System.IO;
using Microsoft.AspNetCore.Http.Connections;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly EmailService _emailService;

        public EmployeesApiController(HrDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
                    GioiTinh = (bool)e.GioiTinh ? "Nam" : "Nữ",
                    e.DiaChi,
                    e.Sdt,
                    e.Email,
                    e.TrinhDoHocVan,
                    e.NgayVaoLam,
                    ChucVu = e.MaChucVuNavigation.TenChucVu,
                    PhongBan = e.MaPhongBanNavigation.TenPhongBan,
                    AvatarUrl = e.AvatarUrl
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

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Kiểm tra số điện thoại đã tồn tại
                    var existingAccount = _context.TaiKhoans.FirstOrDefault(t => t.Username == employeeDto.Sdt);
                    if (existingAccount != null)
                    {
                        return Conflict(new { message = "Số điện thoại đã được sử dụng." });
                    }

                    // Kiểm tra MaPhongBan và MaChucVu
                    if (!_context.PhongBans.Any(p => p.MaPhongBan == employeeDto.MaPhongBan))
                    {
                        return BadRequest(new { message = "Phòng ban không tồn tại." });
                    }
                    if (!_context.ChucVus.Any(c => c.MaChucVu == employeeDto.MaChucVu))
                    {
                        return BadRequest(new { message = "Chức vụ không tồn tại." });
                    }

                    // Tạo nhân viên mới
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

                    // Kiểm tra MaQuyenHan
                    string maQuyenHan = "EMPLOYEE"; // Thay bằng giá trị thực tế từ QuyenHan
                    if (!_context.QuyenHans.Any(q => q.MaQuyenHan == maQuyenHan))
                    {
                        return BadRequest(new { message = $"Mã quyền hạn '{maQuyenHan}' không tồn tại trong bảng QuyenHan." });
                    }

                    string defaultPassword = "123456";
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                    var taiKhoan = new TaiKhoan
                    {
                        Username = employeeDto.Sdt,
                        PasswordHash = hashedPassword,
                        MaQuyenHan = maQuyenHan,
                        MaNv = maNvMoi
                    };

                    _context.TaiKhoans.Add(taiKhoan);
                    _context.SaveChanges();

                    // Xử lý ảnh đại diện (nếu có)
                    if (employeeDto.AvatarUrl != null)
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

                        // ✅ Cập nhật AvatarUrl vào database
                        employee.AvatarUrl = $"/avatars/{fileName}";
                        _context.NhanViens.Update(employee);
                        _context.SaveChanges();
                    }


                    string subject = "Thông tin tài khoản nhân viên";
                    string body = $@"
                        <h3>Chào {employeeDto.HoTen},</h3>
                        <p>Bạn đã được tạo tài khoản nhân viên.</p>
                        <p><strong>Tài khoản:</strong> {taiKhoan.Username}</p>
                        <p><strong>Mật khẩu:</strong> {defaultPassword}</p>
                        <p>Vui lòng đăng nhập và đổi mật khẩu ngay.</p>";

                    _emailService.SendEmail(employeeDto.Email, subject, body);

                    transaction.Commit();

                    // Trả về đối tượng đơn giản để tránh vòng lặp
                    var result = new
                    {
                        id = maNvMoi,
                        HoTen = employee.HoTen,
                        NgaySinh = employee.NgaySinh,
                        GioiTinh = employeeDto.GioiTinh,
                        DiaChi = employee.DiaChi,
                        Sdt = employee.Sdt,
                        Email = employee.Email,
                        TrinhDoHocVan = employee.TrinhDoHocVan,
                        MaPhongBan = employee.MaPhongBan,
                        MaChucVu = employee.MaChucVu
                    };

                    return CreatedAtAction(nameof(GetEmployees), new { id = maNvMoi }, result);
                }
                catch (DbUpdateException dbEx)
                {
                    transaction.Rollback();
                    return StatusCode(500, new { message = dbEx.InnerException?.Message ?? dbEx.Message });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new { message = ex.Message });
                }
            }
        }
        #endregion
    }
}