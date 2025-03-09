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
            if (attendanceData == null || !attendanceData.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
            }

            try
            {
                foreach (var entry in attendanceData)
                {
                    // Kiểm tra ngày làm việc hợp lệ
                    if (!DateOnly.TryParse(entry.NgayLamViec, out DateOnly ngayLamViec))
                    {
                        return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
                    }

                    // Kiểm tra nhân viên đã chấm công ngày này chưa
                    bool daChamCong = await _context.ChamCongs
                        .AnyAsync(c => c.MaNv == entry.MaNv && c.NgayLamViec == ngayLamViec);

                    if (daChamCong)
                    {
                        return BadRequest(new { success = false, message = $"Nhân viên {entry.MaNv} đã chấm công ngày {entry.NgayLamViec}." });
                    }

                    //  Kiểm tra giờ vào
                    TimeOnly? gioVao = null, gioRa = null;
                    if (!string.IsNullOrEmpty(entry.GioVao) && TimeOnly.TryParse(entry.GioVao, out var parsedGioVao))
                        gioVao = parsedGioVao;

                    //  Kiểm tra giờ ra
                    if (!string.IsNullOrEmpty(entry.GioRa) && TimeOnly.TryParse(entry.GioRa, out var parsedGioRa))
                        gioRa = parsedGioRa;

                    //  Kiểm tra tổng giờ
                    decimal tongGio = entry.TongGio ?? 0;

                    var chamCong = new ChamCong
                    {
                        MaNv = entry.MaNv,
                        NgayLamViec = ngayLamViec,
                        GioVao = gioVao,
                        GioRa = gioRa,
                        TongGio = tongGio,
                        TrangThai = entry.TrangThai,
                        GhiChu = entry.GhiChu
                    };

                    Console.WriteLine($"📝 Đang lưu chấm công: NV={chamCong.MaNv}, Ngày={chamCong.NgayLamViec}, Giờ vào={chamCong.GioVao}, Giờ ra={chamCong.GioRa}, Tổng giờ={chamCong.TongGio}");

                    _context.ChamCongs.Add(chamCong);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Chấm công thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Server: {ex}");
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
            public int MaNv { get; set; }
            public string NgayLamViec { get; set; }
            public string? GioVao { get; set; }
            public string? GioRa { get; set; }
            public decimal? TongGio { get; set; }
            public string? TrangThai { get; set; }
            public string? GhiChu { get; set; }
        }
    }
}
