using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Scripting;

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

        // Lấy danh sách nhân viên
        [HttpGet]
        public IActionResult GetEmployees()
        {
            var employees = _context.NhanViens
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
                    e.MaPhongBan
                })
                .ToList();
            return Ok(employees);
        }

        // Xóa nhân viên
        [HttpDelete("{id}")]
        public IActionResult DeleteEmployee(int id)
        {
            var employee = _context.NhanViens.Find(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.NhanViens.Remove(employee);
            _context.SaveChanges();

            return NoContent();
        }

        // Thêm nhân viên
        [HttpPost]
        public IActionResult CreateEmployee([FromBody] NhanVien employee)
        {
            if (employee == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Kiểm tra số điện thoại đã tồn tại chưa
                    var existingAccount = _context.TaiKhoans.FirstOrDefault(t => t.Username == employee.Sdt);
                    if (existingAccount != null)
                    {
                        return Conflict("Số điện thoại đã được sử dụng cho tài khoản khác.");
                    }

                    // Thêm nhân viên vào database
                    _context.NhanViens.Add(employee);
                    _context.SaveChanges();

                    // Mật khẩu mặc định
                    string defaultPassword = "123456";
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                    // Tạo tài khoản nhân viên
                    var taiKhoan = new TaiKhoan
                    {
                        Username = employee.Sdt,  // Username = Số điện thoại
                        PasswordHash = hashedPassword,
                        MaQuyenHan = "2" // Giả sử quyền nhân viên có mã "2"
                    };

                    _context.TaiKhoans.Add(taiKhoan);
                    _context.SaveChanges();

                    // Gửi email tài khoản cho nhân viên
                    string subject = "Thông tin tài khoản nhân viên";
                    string body = $@"
                    <h3>Chào {employee.HoTen},</h3>
                    <p>Bạn đã được tạo tài khoản nhân viên.</p>
                    <p><strong>Tài khoản:</strong> {taiKhoan.Username}</p>
                    <p><strong>Mật khẩu:</strong> {defaultPassword}</p>
                    <p>Vui lòng đăng nhập và đổi mật khẩu ngay.</p>";

                    try
                    {
                        _emailService.SendEmail(employee.Email, subject, body);
                    }
                    catch (Exception emailEx)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "Lỗi khi gửi email: " + emailEx.Message);
                    }

                    transaction.Commit();

                    return CreatedAtAction(nameof(GetEmployees), new { id = employee.MaNv }, employee);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
                }
            }
        }
    }
}
