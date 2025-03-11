using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Scripting;
using HR_KD.DTOs;

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
                    ChucVu = e.MaChucVuNavigation.TenChucVu,
                    PhongBan = e.MaPhongBanNavigation.TenPhongBan
                })
                .ToList();
            return Ok(employees);
        }
        #endregion

        #region Thêm nhân viên
        [HttpPost("CreateEmployee")]
        public IActionResult CreateEmployee([FromForm] CreateEmployeeDTO employeeDto)
        {
            if (employeeDto == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Kiểm tra số điện thoại đã tồn tại
                    var existingAccount = _context.TaiKhoans.FirstOrDefault(t => t.Username == employeeDto.Sdt);
                    if (existingAccount != null)
                    {
                        return Conflict("Số điện thoại đã được sử dụng.");
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
                        MaChucVu = employeeDto.MaChucVu
                    };

                    // Lưu nhân viên vào database trước
                    _context.NhanViens.Add(employee);
                    _context.SaveChanges();

                    // Lấy ID nhân viên vừa tạo
                    int maNvMoi = employee.MaNv;

                    // Tạo tài khoản cho nhân viên mới
                    string defaultPassword = "123456";
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                    var taiKhoan = new TaiKhoan
                    {
                        Username = employeeDto.Sdt,
                        PasswordHash = hashedPassword,
                        MaQuyenHan = "EMPLOYEE",
                        MaNv = maNvMoi // Gán ID nhân viên vào tài khoản
                    };

                    _context.TaiKhoans.Add(taiKhoan);
                    _context.SaveChanges();

                    // Gửi email thông tin tài khoản
                    string subject = "Thông tin tài khoản nhân viên";
                    string body = $@"
                <h3>Chào {employeeDto.HoTen},</h3>
                <p>Bạn đã được tạo tài khoản nhân viên.</p>
                <p><strong>Tài khoản:</strong> {taiKhoan.Username}</p>
                <p><strong>Mật khẩu:</strong> {defaultPassword}</p>
                <p>Vui lòng đăng nhập và đổi mật khẩu ngay.</p>";

                    try
                    {
                        _emailService.SendEmail(employeeDto.Email, subject, body);
                    }
                    catch (Exception emailEx)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "Lỗi khi gửi email: " + emailEx.Message);
                    }

                    transaction.Commit();
                    return CreatedAtAction(nameof(GetEmployees), new { id = maNvMoi }, employee);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }
        #endregion
    }
}
