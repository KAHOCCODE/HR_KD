using HR_KD.Data;
using HR_KD.DTOs;
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

        private int? GetMaNvFromClaims()
        {
            var maNvClaim = User.FindFirst("MaNV")?.Value;
            return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
        }

        // API Chấm công
        [HttpPost]
        [Route("SubmitAttendance")]
        public async Task<IActionResult> SubmitAttendance(List<ChamCongDTO> attendanceData)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
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
                    if (!DateOnly.TryParse(entry.NgayLamViec, out var ngayLamViec))
                    {
                        return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
                    }

                    bool daChamCong = await _context.ChamCongs.AnyAsync(c => c.MaNv == maNv.Value && c.NgayLamViec == ngayLamViec);
                    if (daChamCong)
                    {
                        return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã chấm công ngày {entry.NgayLamViec}." });
                    }

                    var chamCong = new ChamCong
                    {
                        MaNv = maNv.Value,
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
        public async Task<IActionResult> GetAttendanceRecords(string? ngayLamViec = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.ChamCongs.Where(c => c.MaNv == maNv.Value);

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
        [HttpGet("GetAttendanceRequests")]
        public async Task<IActionResult> GetAttendanceRequests(string? ngayLamViec = null)
        {
            // Lấy mã nhân viên từ claims của người dùng đăng nhập
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.YeuCauSuaChamCongs.Where(c => c.MaNv == maNv.Value);

            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
            {
                query = query.Where(c => c.NgayLamViec == ngay);
            }

            var requests = await query
                .Select(c => new
                {
                    c.MaYeuCau,
                    c.NgayLamViec,
                    GioVaoMoi = c.GioVaoMoi.HasValue ? c.GioVaoMoi.Value.ToString("HH:mm") : null,
                    GioRaMoi = c.GioRaMoi.HasValue ? c.GioRaMoi.Value.ToString("HH:mm") : null,
                    c.LyDo,
                    c.TrangThai
                })
                .ToListAsync();

            return Ok(new { success = true, requests });
        }
        [HttpPost("AcceptAttendanceRequest")]
        public async Task<IActionResult> AcceptAttendanceRequest([FromBody] AcceptAttendanceRequestDto request)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var yeuCau = await _context.YeuCauSuaChamCongs
                .FirstOrDefaultAsync(y => y.MaYeuCau == request.MaYeuCau && y.MaNv == maNv.Value);

            if (yeuCau == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy yêu cầu chấm công." });
            }

            try
            {
                foreach (var entry in request.AttendanceData)
                {
                    if (!DateOnly.TryParse(entry.NgayLamViec, out var ngayLamViec))
                    {
                        return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
                    }

                    var chamCong = await _context.ChamCongs
                        .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.NgayLamViec == ngayLamViec);

                    if (chamCong == null)
                    {
                        chamCong = new ChamCong
                        {
                            MaNv = maNv.Value,
                            NgayLamViec = ngayLamViec,
                            TrangThai = "Đã duyệt",
                            GhiChu = "Chấp nhận yêu cầu chấm công"
                        };
                        _context.ChamCongs.Add(chamCong);
                    }

                    // Cập nhật giờ vào/ra từ yêu cầu
                    chamCong.GioVao = yeuCau.GioVaoMoi ?? chamCong.GioVao;
                    chamCong.GioRa = yeuCau.GioRaMoi ?? chamCong.GioRa;

                    // Tính toán tổng giờ tự động
                    if (chamCong.GioVao.HasValue && chamCong.GioRa.HasValue)
                    {
                        chamCong.TongGio = (decimal)(chamCong.GioRa.Value - chamCong.GioVao.Value).TotalHours;
                    }
                }

                // Xóa yêu cầu sau khi xử lý thành công [3][9]
                _context.YeuCauSuaChamCongs.Remove(yeuCau);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Xử lý yêu cầu thành công",
                    deletedRequestId = yeuCau.MaYeuCau
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống",
                    error = ex.Message
                });
            }
        }


        // Thêm DTO cho request
        public class AcceptAttendanceRequestDto
        {
            public int MaYeuCau { get; set; }
            public List<ChamCongRequestDto> AttendanceData { get; set; }
        }

        public class ChamCongRequestDto
        {
            public string NgayLamViec { get; set; }
            public string GioVao { get; set; }
            public string GioRa { get; set; }
        }







    }
}
