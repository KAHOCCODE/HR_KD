using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollDirectorApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly Dictionary<string, string> _statusNameMapping;
        private readonly EmailService _emailService;
        private readonly PayrollReportService _reportService;

        public PayrollDirectorApiController(HrDbContext context, EmailService emailService, PayrollReportService reportService)
        {
            _context = context;
            _emailService = emailService;
            _reportService = reportService;
            _statusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
        }

        #region API cho Giám đốc
        [HttpGet("GetPayrolls")]
        public async Task<IActionResult> GetPayrolls(int year, int month)
        {
            var bangLuongs = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.TrangThai == "BL3") 
                .ToListAsync();

            var result = bangLuongs.Select(b => new
            {
                b.MaLuong,
                b.MaNv,
                HoTen = b.MaNvNavigation.HoTen,
                b.ThucNhan,
                b.TrangThai,
                TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
            });

            return Ok(result);
        }

        [HttpPost("FinalApprove")]
        public async Task<IActionResult> FinalApprove([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
               .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3") 
               .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("One or more payrolls are not in the appropriate state for final approval.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL4"; // BL4: Finally Approved by Director
                b.NguoiDuyetGD = currentUser; // Assuming you have a field for Director approver
                b.NgayDuyetGD = currentTime; // Assuming you have a field for Director approval date
            }

            await _context.SaveChangesAsync();
            // Consider adding logging here
            return Ok("Payrolls finally approved by Director successfully.");
        }

        [HttpPost("ReturnToAccountant")]
        public async Task<IActionResult> ReturnToAccountant([FromBody] int[] maLuongList, [FromQuery] string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
                return BadRequest("Reason for return is required.");

            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3") // Only return if in "Accountant Approved" state
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("One or more payrolls are not in the appropriate state to be returned.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3R"; // BL3R: Returned by Director
                b.GhiChu = lyDo;
                b.NguoiTraVeGD = currentUser; // Assuming you have a field for Director returner
                b.NgayTraVeTuGD = currentTime; // Assuming you have a field for Director return date
            }

            await _context.SaveChangesAsync();
            // Consider adding logging here
            return Ok("Payrolls returned to Accountant successfully.");
        }

        #region API để gửi bảng lương cho nhân viên (sau khi giám đốc duyệt)
        [HttpPost("SendToEmployees")]
        public async Task<IActionResult> SendToEmployees([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
               .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL4")
               .Include(b => b.MaNvNavigation)
                   .ThenInclude(nv => nv.MaPhongBanNavigation)
               .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái 'Đã duyệt giám đốc' (BL4).");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;
            var errors = new List<string>();

            foreach (var payroll in luongs)
            {
                try
                {
                    var pdfBytes = await _reportService.GeneratePayrollPdf(payroll.MaLuong);

                    var nv = payroll.MaNvNavigation;
                    if (string.IsNullOrWhiteSpace(nv.Email))
                    {
                        errors.Add($"Nhân viên {nv.MaNv} ({nv.HoTen}) chưa có email.");
                        continue;
                    }

                    var subject = $"[BẢNG LƯƠNG] Tháng {payroll.ThangNam:MM/yyyy}";
                    var body = $"Xin chào <b>{nv.HoTen}</b>,<br><br>" +
                               $"Bảng lương tháng <b>{payroll.ThangNam:MM/yyyy}</b> của bạn đã được phê duyệt và đính kèm trong email này.<br>" +
                               $"Vui lòng kiểm tra kỹ và phản hồi nếu có thắc mắc trước hạn quy định.<br><br>" +
                               $"Trân trọng.";

                    _emailService.SendEmail(nv.Email, subject, body, pdfBytes, $"BangLuong_{payroll.MaLuong}.pdf");

                    // Cập nhật trạng thái sau khi gửi
                    payroll.TrangThai = "BL5";
                    payroll.NgayGuiNV = currentTime;
                    payroll.NguoiGuiNV = currentUser;
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi gửi cho nhân viên {payroll.MaNv}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            if (errors.Any())
                return Ok(new { message = "Một số bảng lương đã gửi nhưng có lỗi:", errors });

            return Ok("Tất cả bảng lương đã được gửi thành công.");
        }

        #endregion
        #endregion
    }
}
