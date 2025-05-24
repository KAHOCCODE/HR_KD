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

        // Lấy danh sách bảng lương từ BL1 trở lên
        [HttpGet("GetPayrolls")]
        public async Task<IActionResult> GetPayrolls(int year, int month)
        {
            var bangLuongs = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month &&
                            (b.TrangThai == "BL1" || b.TrangThai == "BL1A" || b.TrangThai == "BL2" || 
                             b.TrangThai == "BL3" || b.TrangThai == "BL4" || b.TrangThai == "BL5"))
                .ToListAsync();

            var result = bangLuongs.Select(b => new
            {
                b.MaLuong,
                b.MaNv,
                HoTen = b.MaNvNavigation.HoTen,
                b.ThucNhan,
                b.TrangThai,
                TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai,
                NguoiDuyetGD = b.NguoiDuyetGD,
                NgayDuyetGD = b.NgayDuyetGD,
                NguoiGuiNV = b.NguoiGuiNV,
                NgayGuiNV = b.NgayGuiNV
            });

            return Ok(result);
        }

        // Duyệt từng bảng lương (BL3 => BL4)
        [HttpPost("FinalApprove")]
        public async Task<IActionResult> FinalApprove([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3")
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một số bảng lương không ở trạng thái BL3 để duyệt.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL4"; // Giám đốc duyệt
                b.NguoiDuyetGD = currentUser;
                b.NgayDuyetGD = currentTime;
            }

            await _context.SaveChangesAsync();
            return Ok("Đã duyệt các bảng lương thành công.");
        }

        // Trả lại kế toán
        [HttpPost("ReturnToAccountant")]
        public async Task<IActionResult> ReturnToAccountant([FromBody] int[] maLuongList, [FromQuery] string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
                return BadRequest("Lý do trả về là bắt buộc.");

            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3")
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một số bảng lương không ở trạng thái phù hợp để trả về.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3R"; // Trả về kế toán
                b.GhiChu = lyDo;
                b.NguoiTraVeGD = currentUser;
                b.NgayTraVeTuGD = currentTime;
            }

            await _context.SaveChangesAsync();
            return Ok("Đã trả về cho kế toán thành công.");
        }

        // Gửi tất cả cho nhân viên (BL4 hoặc BL3 được duyệt trực tiếp ➜ BL5)
        [HttpPost("SendToEmployees")]
        public async Task<IActionResult> SendToEmployees([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && (b.TrangThai == "BL3" || b.TrangThai == "BL4"))
                .Include(b => b.MaNvNavigation)
                    .ThenInclude(nv => nv.MaPhongBanNavigation)
                .ToListAsync();

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;
            var errors = new List<string>();

            foreach (var payroll in luongs)
            {
                try
                {
                    // Nếu chưa được giám đốc duyệt (BL3), cập nhật duyệt trước khi gửi
                    if (payroll.TrangThai == "BL3")
                    {
                        payroll.TrangThai = "BL4";
                        payroll.NguoiDuyetGD = currentUser;
                        payroll.NgayDuyetGD = currentTime;
                    }

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
                               $"Vui lòng kiểm tra kỹ và phản hồi nếu có thắc mắc trước hạn quy định.<br><br>Trân trọng.";

                    _emailService.SendEmail(nv.Email, subject, body, pdfBytes, $"BangLuong_{payroll.MaLuong}.pdf");

                    // Cập nhật trạng thái gửi mail
                    payroll.TrangThai = "BL5";
                    payroll.NgayGuiNV = currentTime;
                    payroll.NguoiGuiNV = currentUser;
                }
                catch (Exception ex)
                {
                    errors.Add($"Lỗi gửi bảng lương {payroll.MaLuong}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            if (errors.Any())
                return Ok(new { message = "Một số bảng lương gặp lỗi khi gửi:", errors });

            return Ok("Tất cả bảng lương đã được gửi thành công.");
        }

        #endregion
    }
}
