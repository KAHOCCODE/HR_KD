using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    [Route("api/Attendance")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly HrDbContext _context;

        public AttendanceController(HrDbContext context)
        {
            _context = context;
        }

        // API Chấm công
        [HttpPost]
        [Route("SubmitAttendance")]
        public async Task<IActionResult> SubmitAttendance([FromBody] List<ChamCongDto> attendanceData)
        {
            var maNvClaim = User.FindFirst("MaNV")?.Value;
            if (string.IsNullOrEmpty(maNvClaim) || !int.TryParse(maNvClaim, out int maNv))
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            if (attendanceData == null || !attendanceData.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
            }

            try
            {
                foreach (var entry in attendanceData)
                {
                    DateOnly ngayLamViec;
                    if (!DateOnly.TryParse(entry.NgayLamViec, out ngayLamViec))
                    {
                        return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
                    }

                    bool daChamCong = await _context.ChamCongs.AnyAsync(c => c.MaNv == maNv && c.NgayLamViec == ngayLamViec);
                    if (daChamCong)
                    {
                        return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã chấm công ngày {entry.NgayLamViec}." });
                    }

                    var chamCong = new ChamCong
                    {
                        MaNv = maNv, // 🚀 Lấy từ Claim, không nhận từ frontend
                        NgayLamViec = ngayLamViec,
                        GioVao = TimeOnly.TryParse(entry.GioVao, out var parsedGioVao) ? parsedGioVao : null,
                        GioRa = TimeOnly.TryParse(entry.GioRa, out var parsedGioRa) ? parsedGioRa : null,
                        TongGio = entry.TongGio ?? 0,
                        TrangThai = entry.TrangThai,
                        GhiChu = entry.GhiChu
                    };

                    _context.ChamCongs.Add(chamCong);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Chấm công thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        // API Lấy dữ liệu chấm công
        [HttpGet]
        [Route("GetAttendanceRecords")]
        public async Task<IActionResult> GetAttendanceRecords(int maNv, string? ngayLamViec = null)
        {
            var query = _context.ChamCongs.Where(c => c.MaNv == maNv);

            // Nếu có ngày làm việc, lọc theo ngày
            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
            {
                query = query.Where(c => c.NgayLamViec == ngay);
            }

            var records = await query
                .Select(c => new
                {
                    c.NgayLamViec,
                    GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString("HH:mm") : null,
                    GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString("HH:mm") : null,
                    c.TongGio,
                    c.TrangThai,
                    c.GhiChu
                })
                .ToListAsync();

            return Ok(new { success = true, records });
        }

        // DTO dùng để nhận dữ liệu từ frontend
        public class ChamCongDto
        {
            public string NgayLamViec { get; set; }
            public string? GioVao { get; set; }
            public string? GioRa { get; set; }
            public decimal? TongGio { get; set; }
            public string? TrangThai { get; set; }
            public string? GhiChu { get; set; }
        }
    }
}
