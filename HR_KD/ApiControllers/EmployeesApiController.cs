using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.DTOs;
using System.IO;
using System.Collections.Generic;
using HR_KD.Helpers;
using Humanizer;
using HR_KD.Services;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly EmailService _emailService;
        private readonly UsernameGeneratorService _usernameGen;
        private readonly ILogger<EmployeesApiController> _logger;

        public EmployeesApiController(HrDbContext context, EmailService emailService, ILogger<EmployeesApiController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _usernameGen = new UsernameGeneratorService();
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
                string username = _usernameGen.GenerateUsername(string.IsNullOrEmpty(employeeDto.HoTen) ? "user" : employeeDto.HoTen, maNvMoi);
                string defaultPassword = "123456";
                string randomkey = PasswordHelper.GenerateRandomKey();
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword + randomkey);

                var taiKhoan = new TaiKhoan
                {
                    Username =username,
                    PasswordHash = hashedPassword,
                    PasswordSalt = randomkey,
                    MaNv = maNvMoi
                };
                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();

                // ✅ Gán quyền hạn cho tài khoản
                foreach (var role in validRoles)
                {
                    _context.TaiKhoanQuyenHans.Add(new TaiKhoanQuyenHan
                    {
                        Username = taiKhoan.Username, 
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
                    <table width='100%' cellpadding='0' cellspacing='0' style='font-family:Segoe UI, sans-serif; background: #f4f4f4; padding: 30px;'>
                        <tr>
                            <td align='center'>
                                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 0 10px rgba(0,0,0,0.05);'>
                                    <tr>
                                        <td style='background-color: #004080; padding: 20px 0; text-align: center;'>
                                            <img src='' alt='Company Logo' height='50' />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 30px;'>
                                            <h2 style='color: #004080;'>Chào {employeeDto.HoTen},</h2>
                                            <p>Chúc mừng bạn đã trở thành một phần của công ty 🎉.</p>
                                            <p>Dưới đây là thông tin tài khoản để bạn có thể đăng nhập vào hệ thống:</p>

                                            <table cellpadding='8' cellspacing='0' width='100%' style='margin-top: 20px; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;'>
                                                <tr style='background-color: #f0f0f0;'>
                                                    <th align='left'>Tài khoản</th>
                                                    <th align='left'>Mật khẩu tạm thời</th>
                                                </tr>
                                                <tr>
                                                    <td>{taiKhoan.Username}</td>
                                                    <td>{defaultPassword}</td>
                                                </tr>
                                            </table>

                                            <p style='margin-top: 20px; color: #888;'>
                                                * Vui lòng đăng nhập vào hệ thống và đổi mật khẩu để đảm bảo an toàn.
                                            </p>

                                            <div style='text-align:center; margin: 30px 0;'>
                                                <a href='' 
                                                    style='display:inline-block; background-color:#004080; color:white; padding:12px 20px; text-decoration:none; border-radius:5px; font-weight:bold;'>
                                                    Đăng nhập ngay
                                                </a>
                                            </div>

                                            <p style='font-size: 13px; color: #999; text-align: center;'>
                                                Nếu bạn có bất kỳ câu hỏi nào, hãy liên hệ với bộ phận IT để được hỗ trợ.
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style='background-color: #eeeeee; padding: 15px; text-align: center; font-size: 12px; color: #777;'>
                                            &copy; 2025 Công ty ABC | <a href='https://yourcompanydomain.com' style='color:#004080;'>Trang chủ</a>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>";


                    _emailService.SendEmail(employeeDto.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email.");
                }

                transaction.Commit();

                // Tạo DTO để trả về
                var responseDto = new
                {
                    MaNv = employee.MaNv,
                    HoTen = employee.HoTen,
                    NgaySinh = employee.NgaySinh,
                    GioiTinh = employee.GioiTinh.HasValue ? (employee.GioiTinh.Value ? "Nam" : "Nữ") : "Không xác định",
                    DiaChi = employee.DiaChi,
                    Sdt = employee.Sdt,
                    Email = employee.Email,
                    TrinhDoHocVan = employee.TrinhDoHocVan,
                    NgayVaoLam = employee.NgayVaoLam,
                    MaPhongBan = employee.MaPhongBan,
                    MaChucVu = employee.MaChucVu,
                    AvatarUrl = employee.AvatarUrl
                };

                return CreatedAtAction(nameof(GetEmployees), new { id = maNvMoi }, responseDto);
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
