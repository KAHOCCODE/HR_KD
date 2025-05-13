using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HR_KD.Services; // Import the service

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "EMPLOYEE")]
    public class PayrollEmployeeApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly Dictionary<string, string> _statusNameMapping;
        private readonly PayrollReportService _payrollReportService; // Inject the service

        public PayrollEmployeeApiController(HrDbContext context, PayrollReportService payrollReportService)
        {
            _context = context;
            _statusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
            _payrollReportService = payrollReportService; // Initialize the service
        }

        #region Bảng lương cá nhân (cho nhân viên) --> chấp nhận hoặc từ chối bảng lương
        [HttpGet("MyPayroll")]
        public async Task<IActionResult> GetMyPayroll()
        {
            var username = User.Identity?.Name;
            var user = await _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .FirstOrDefaultAsync(t => t.Username == username);

            if (user == null) return Unauthorized();

            int employeeId = user.MaNv;

            var payrolls = await _context.BangLuongs
                .Where(b => b.MaNv == employeeId)
                .OrderByDescending(b => b.ThangNam)
                .ToListAsync();

            if (!payrolls.Any())
            {
                return Ok(new { hasPayroll = false, message = "Chưa có bảng lương nào." });
            }

            var result = payrolls
                .GroupBy(p => p.ThangNam.Year)
                .OrderByDescending(g => g.Key)
                .Select(yearGroup => new
                {
                    Year = yearGroup.Key,
                    Months = yearGroup.OrderByDescending(p => p.ThangNam.Month)
                                            .Select(p => new
                                            {
                                                p.MaLuong,
                                                MonthYear = p.ThangNam.ToString("yyyy-MM"),
                                                DisplayMonthYear = $"Tháng {p.ThangNam.Month} - {p.ThangNam.Year}",
                                                p.TongLuong,
                                                p.ThucNhan,
                                                p.TrangThai,
                                                TenTrangThai = _statusNameMapping.TryGetValue(p.TrangThai, out var name) ? name : p.TrangThai,
                                                p.LuongTangCa,
                                                p.LuongThem,
                                                p.PhuCapThem,
                                                p.ThueTNCN
                                            })
                });

            return Ok(new { hasPayroll = true, data = result });
        }

        [HttpPost("ConfirmPayroll/{maLuong}")]
        public async Task<IActionResult> ConfirmPayroll(int maLuong)
        {
            var username = User.Identity?.Name;
            var user = await _context.TaiKhoans
                                            .Include(t => t.MaNvNavigation)
                                            .FirstOrDefaultAsync(t => t.Username == username);

            if (user == null) return Unauthorized();

            var payroll = await _context.BangLuongs.FindAsync(maLuong);
            if (payroll == null) return NotFound("Không tìm thấy bảng lương.");

            // Verify if the payroll belongs to the current employee
            if (payroll.MaNv != user.MaNv)
            {
                return Forbid("Bạn không có quyền xác nhận bảng lương này."); // Use Forbid for authorization issues
            }

            if (payroll.TrangThai != "BL1")
                return BadRequest("Không thể xác nhận bảng lương ở trạng thái hiện tại.");

            payroll.TrangThai = "BL1A"; // Nhân viên xác nhận
            payroll.NguoiDuyetNV = user.Username; // Assuming you have a field for employee confirmer
            payroll.NgayDuyetNV = DateTime.Now; // Assuming you have a field for employee confirmation date

            await _context.SaveChangesAsync();
            return Ok("Bảng lương đã được xác nhận.");
        }

        [HttpPost("RejectPayroll/{maLuong}")]
        public async Task<IActionResult> RejectPayroll(int maLuong, [FromBody] string? lyDo)
        {
            var username = User.Identity?.Name;
            var user = await _context.TaiKhoans
                                            .Include(t => t.MaNvNavigation)
                                            .FirstOrDefaultAsync(t => t.Username == username);

            if (user == null) return Unauthorized();

            var payroll = await _context.BangLuongs.FindAsync(maLuong);
            if (payroll == null) return NotFound("Không tìm thấy bảng lương.");

            // Verify if the payroll belongs to the current employee
            if (payroll.MaNv != user.MaNv)
            {
                return Forbid("Bạn không có quyền từ chối bảng lương này."); // Use Forbid for authorization issues
            }


            if (payroll.TrangThai != "BL1") // Chỉ được từ chối khi ở trạng thái "Đã tạo"
                return BadRequest("Không thể từ chối bảng lương ở trạng thái hiện tại.");

            payroll.TrangThai = "BL1R"; // Nhân viên từ chối
            payroll.GhiChu = lyDo;
            payroll.NguoiTuChoiNV = user.Username; // Assuming you have a field for employee rejector
            payroll.NgayTuChoiNV = DateTime.Now; // Assuming you have a field for employee rejection date

            await _context.SaveChangesAsync();
            return Ok("Đã gửi yêu cầu điều chỉnh.");
        }
        #endregion

        #region Tạo báo cáo chi tiết
        [HttpGet("CreatePayrollDetailReport/{maLuong}")]
        public async Task<IActionResult> CreatePayrollDetailReport(int maLuong)
        {
            try
            {
                byte[] pdfBytes = await _payrollReportService.GeneratePayrollPdf(maLuong);
                return File(pdfBytes, "application/pdf", $"BangLuong_{maLuong}.pdf"); // Tên file có thể tùy chỉnh
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi khi tạo báo cáo: {ex.Message}");
            }
        }
        #endregion
    }
}
