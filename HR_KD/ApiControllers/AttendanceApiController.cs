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

        private (DateOnly start, DateOnly end) GetWeekRange(DateOnly date)
        {
            DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);
            int dayOfWeek = (int)dateTime.DayOfWeek;
            int daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            DateOnly start = date.AddDays(-daysToMonday);
            DateOnly end = start.AddDays(6);
            return (start, end);
        }

        [HttpGet("GetOvertimeRates")]
        public async Task<IActionResult> GetOvertimeRates()
        {
            try
            {
                var rates = await _context.TiLeTangCas
                    .Where(t => t.KichHoat)
                    .Select(t => new { t.Id, t.TenTiLeTangCa, t.TiLe })
                    .ToListAsync();
                return Ok(new { success = true, rates });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("SubmitAttendance")]
        public async Task<IActionResult> SubmitAttendance(AttendanceDataDTO data)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            if (data == null || (data.attendance == null || !data.attendance.Any()) && (data.overtime == null || !data.overtime.Any()))
            {
                return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
            }

            try
            {
                // Attendance processing logic
                if (data.attendance != null && data.attendance.Any())
                {
                    foreach (var entry in data.attendance)
                    {
                        if (!DateOnly.TryParse(entry.NgayLamViec, out var ngayLamViec))
                        {
                            return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
                        }

                        // Check if employee already submitted attendance for this date
                        bool daChamCong = await _context.LichSuChamCongs.AnyAsync(c => c.MaNv == maNv.Value && c.Ngay == ngayLamViec);
                        if (daChamCong)
                        {
                            return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã chấm công ngày {entry.NgayLamViec}." });
                        }

                        // Check if employee has a leave record for this date
                        var leaveRecord = await _context.NgayNghis
                            .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.NgayNghi1 == ngayLamViec);
                        if (leaveRecord != null)
                        {
                            // Prevent attendance if leave status is not NN4
                            if (leaveRecord.MaTrangThai != "NN4")
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = $"Nhân viên {maNv} có ngày nghỉ với trạng thái {leaveRecord.MaTrangThai} vào ngày {entry.NgayLamViec}, không thể chấm công.",
                                    error = "Employee on leave with non-NN4 status"
                                });
                            }
                        }

                        // Check weekly working hours limit
                        var (start, end) = GetWeekRange(ngayLamViec);
                        var weeklyHours = await _context.LichSuChamCongs
                            .Where(c => c.MaNv == maNv.Value && c.Ngay >= start && c.Ngay <= end)
                            .SumAsync(c => c.TongGio ?? 0);
                        if (weeklyHours >= 40)
                        {
                            return BadRequest(new { success = false, message = $"Đã đủ 40 giờ làm việc trong tuần bắt đầu từ {start}, không thể chấm công thêm." });
                        }

                        var chamCong = new LichSuChamCong
                        {
                            MaNv = maNv.Value,
                            Ngay = ngayLamViec,
                            GioVao = TimeOnly.TryParse(entry.GioVao, out var parsedGioVao) ? parsedGioVao : null,
                            GioRa = TimeOnly.TryParse(entry.GioRa, out var parsedGioRa) ? parsedGioRa : null,
                            TongGio = entry.TongGio ?? 0,
                            TrangThai = entry.TrangThai,
                            GhiChu = entry.GhiChu
                        };
                        _context.LichSuChamCongs.Add(chamCong);
                    }
                    await _context.SaveChangesAsync();
                }

                // Overtime processing logic (unchanged)
                if (data.overtime != null && data.overtime.Any())
                {
                    foreach (var entry in data.overtime)
                    {
                        if (!DateOnly.TryParse(entry.NgayTangCa, out var ngayTangCa))
                        {
                            return BadRequest(new { success = false, message = $"Ngày tăng ca không hợp lệ: {entry.NgayTangCa}" });
                        }

                        if (!TimeOnly.TryParse(entry.GioVaoTangCa, out var gioVaoTangCa) || !TimeOnly.TryParse(entry.GioRaTangCa, out var gioRaTangCa))
                        {
                            return BadRequest(new { success = false, message = $"Giờ vào hoặc giờ ra tăng ca không hợp lệ cho ngày {entry.NgayTangCa}." });
                        }

                        var (start, end) = GetWeekRange(ngayTangCa);
                        var weeklyOvertimeHours = await _context.TangCas
                            .Where(c => c.MaNv == maNv.Value && c.NgayTangCa >= start && c.NgayTangCa <= end)
                            .SumAsync(c => c.SoGioTangCa);
                        if (weeklyOvertimeHours >= 12)
                        {
                            return BadRequest(new { success = false, message = $"Đã đủ 12 giờ tăng ca trong tuần bắt đầu từ {start}, không thể tăng ca thêm." });
                        }

                        var lamBuRecords = await _context.LamBus
                            .Where(lb => lb.MaNV == maNv.Value && lb.NgayLamViec == ngayTangCa && lb.GioVao.HasValue && lb.GioRa.HasValue)
                            .ToListAsync();

                        foreach (var lamBu in lamBuRecords)
                        {
                            var gioVaoLamBu = lamBu.GioVao.Value;
                            var gioRaLamBu = lamBu.GioRa.Value;

                            if (gioVaoTangCa < gioRaLamBu && gioRaTangCa > gioVaoLamBu)
                            {
                                return BadRequest(new
                                {
                                    success = false,
                                    message = $"Giờ tăng ca từ {gioVaoTangCa:HH:mm} đến {gioRaTangCa:HH:mm} trùng với giờ làm bù từ {gioVaoLamBu:HH:mm} đến {gioRaLamBu:HH:mm} vào ngày {ngayTangCa}."
                                });
                            }
                        }

                        var tangCa = new TangCa
                        {
                            MaNv = maNv.Value,
                            NgayTangCa = ngayTangCa,
                            SoGioTangCa = entry.SoGioTangCa,
                            TyLeTangCa = (decimal)(entry.TiLeTangCa / 100.0),
                            GhiChu = entry.GhiChu,
                            TrangThai = "TC1",
                            GioVao = gioVaoTangCa,
                            GioRa = gioRaTangCa,
                            MaNvDuyet = 0
                        };
                        _context.TangCas.Add(tangCa);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Chấm công thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống.",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        [Route("GetOvertimeRecords")]
        public async Task<IActionResult> GetOvertimeRecords(string? ngayTangCa = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.TangCas.Where(c => c.MaNv == maNv.Value);

            if (!string.IsNullOrEmpty(ngayTangCa) && DateOnly.TryParse(ngayTangCa, out DateOnly ngay))
            {
                query = query.Where(c => c.NgayTangCa == ngay);
            }

            var records = await query
                .Select(c => new
                {
                    c.NgayTangCa,
                    c.SoGioTangCa,
                    c.TrangThai
                })
                .ToListAsync();

            return Ok(new { success = true, records });
        }

        [HttpGet]
        [Route("GetAttendanceRecords")]
        public async Task<IActionResult> GetAttendanceRecords(string? ngayLamViec = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.ChamCongs.Where(c => c.MaNv == maNv.Value && c.TrangThai == "CC2");

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

        [HttpGet]
        [Route("GetAttendanceHistoryRecords")]
        public async Task<IActionResult> GetAttendanceHistoryRecords(string? ngayLamViec = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.LichSuChamCongs.Where(c => c.MaNv == maNv.Value);

            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
            {
                query = query.Where(c => c.Ngay == ngay);
            }

            var records = await query
                .Select(c => new
                {
                    c.Ngay,
                    GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString("HH:mm") : null,
                    GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString("HH:mm") : null,
                    c.TongGio,
                    c.TrangThai,
                    c.GhiChu
                })
                .ToListAsync();

            return Ok(new { success = true, records });
        }

        [HttpGet]
        [Route("GetAttendanceHistoryRecords2")]
        public async Task<IActionResult> GetAttendanceHistoryRecords2(string? ngayLamViec = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.LichSuChamCongs.Where(c => c.MaNv == maNv.Value && c.TrangThai == "LS1");

            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
            {
                query = query.Where(c => c.Ngay == ngay);
            }

            var records = await query
                .Select(c => new
                {
                    c.Ngay,
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
        public async Task<IActionResult> AcceptAttendanceRequest(AcceptAttendanceRequestDTO request)
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
                            TrangThai = "CC2",
                            GhiChu = "Chấp nhận yêu cầu chấm công"
                        };
                        _context.ChamCongs.Add(chamCong);
                    }

                    chamCong.GioVao = yeuCau.GioVaoMoi ?? chamCong.GioVao;
                    chamCong.GioRa = yeuCau.GioRaMoi ?? chamCong.GioRa;

                    if (chamCong.GioVao.HasValue && chamCong.GioRa.HasValue)
                    {
                        chamCong.TongGio = (decimal)(chamCong.GioRa.Value - chamCong.GioVao.Value).TotalHours;
                    }
                }

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

        [HttpGet("GetStatusForMonth")]
        public async Task<IActionResult> GetStatusForMonth(int maNV, string? monthYear = null)
        {
            DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                ? DateTime.Now
                : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                    ? parsedMonth
                    : DateTime.Now;

            var records = await _context.ChamCongs
                .Where(c => c.MaNv == maNV &&
                            c.NgayLamViec.Year == selectedMonth.Year &&
                            c.NgayLamViec.Month == selectedMonth.Month &&
                            c.TrangThai.Trim() == "CC2")
                .ToListAsync();

            if (!records.Any())
            {
                var historyRecords = await _context.LichSuChamCongs
                    .Where(c => c.MaNv == maNV &&
                                c.Ngay.Year == selectedMonth.Year &&
                                c.Ngay.Month == selectedMonth.Month &&
                                c.TrangThai.Trim() == "LS2")
                    .ToListAsync();

                if (!historyRecords.Any())
                {
                    return Ok(new { status = "Chưa có dữ liệu chấm công đã duyệt" });
                }

                var historyApprovedCount = historyRecords.Count;
                var historyTotalDays = historyRecords.Count;
                return Ok(new { status = $"Lịch sử: {historyApprovedCount}/{historyTotalDays} ngày đã duyệt" });
            }

            var approvedCount = records.Count;
            var totalDays = records.Count;

            return Ok(new { status = $"{approvedCount}/{totalDays} ngày đã duyệt" });
        }

        [HttpPost("RejectAttendanceRequest")]
        public async Task<IActionResult> RejectAttendanceRequest(RejectAttendanceRequestDTO request)
        {
            try
            {
                var requestRecord = await _context.YeuCauSuaChamCongs
                    .FirstOrDefaultAsync(a => a.MaYeuCau == request.MaYeuCau);

                if (requestRecord == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy yêu cầu chấm công." });
                }

                if (request.NgayLamViec == default(DateOnly) || request.NgayLamViec == DateOnly.MinValue)
                {
                    return BadRequest(new { success = false, message = "Ngày làm việc không hợp lệ." });
                }

                var ngayNghi = new NgayNghi
                {
                    MaNv = requestRecord.MaNv,
                    NgayNghi1 = request.NgayLamViec,
                    LyDo = !string.IsNullOrEmpty(request.LyDo) ? request.LyDo : "Không có lý do cụ thể",
                    MaLoaiNgayNghi = request.MaLoaiNgayNghi,
                    NgayLamDon = DateTime.Now
                };

                _context.NgayNghis.Add(ngayNghi);
                await _context.SaveChangesAsync();

                _context.YeuCauSuaChamCongs.Remove(requestRecord);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Yêu cầu chấm công đã bị từ chối và lưu vào NgayNghi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpGet("GetHolidays")]
        public async Task<IActionResult> GetHolidays()
        {
            try
            {
                var holidays = await _context.NgayLes
                    .Where(n => n.TrangThai == "NL4")
                    .Select(n => new
                    {
                        ngayLe = n.NgayLe1.ToString("yyyy-MM-dd"),
                        tenNgayLe = n.TenNgayLe,
                        trangThai = n.TrangThai,
                        soNgayNghi = n.SoNgayNghi
                    })
                    .ToListAsync();

                return Ok(new { success = true, holidays });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetAttendanceCalendarRecords")]
        public async Task<IActionResult> GetAttendanceCalendarRecords(string? ngayLamViec = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            var query = _context.ChamCongs.Where(c => c.MaNv == maNv.Value && (c.TrangThai == "CC3"));

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

        [HttpGet]
        [Route("GetApprovedRecords")]
        public async Task<IActionResult> GetApprovedRecords(string? startDate = null, string? endDate = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // Define the base queries with aligned anonymous types
                var chamCongQuery = _context.ChamCongs
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "CC3")
                    .Select(c => new
                    {
                        Loai = "Chấm Công",
                        Ngay = c.NgayLamViec, // DateOnly
                        GioVao = c.GioVao, // TimeOnly?
                        GioRa = c.GioRa, // TimeOnly?
                        TongGio = c.TongGio, // decimal?
                        GhiChu = c.GhiChu ?? "", // string
                        TrangThai = c.TrangThai // string
                    });

                var tangCaQuery = _context.TangCas
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "TC3")
                    .Select(c => new
                    {
                        Loai = "Tăng Ca",
                        Ngay = c.NgayTangCa, // DateOnly
                        GioVao = c.GioVao, // TimeOnly?
                        GioRa = c.GioRa, // TimeOnly?
                        TongGio = (decimal?)c.SoGioTangCa, // Cast to decimal? to align types
                        GhiChu = c.GhiChu ?? "", // string
                        TrangThai = c.TrangThai // string
                    });

                var lamBuQuery = _context.LamBus
                    .Where(c => c.MaNV == maNv.Value && c.TrangThai == "LB3")
                    .Select(c => new
                    {
                        Loai = "Làm Bù",
                        Ngay = c.NgayLamViec, // DateOnly
                        GioVao = c.GioVao, // TimeOnly?
                        GioRa = c.GioRa, // TimeOnly?
                        TongGio = c.TongGio, // decimal?
                        GhiChu = c.GhiChu ?? "", // string
                        TrangThai = c.TrangThai // string
                    });

                // Combine queries with Union
                var query = chamCongQuery
                    .Union(tangCaQuery)
                    .Union(lamBuQuery);

                // Apply date filtering if provided
                if (DateOnly.TryParse(startDate, out DateOnly start) && DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(r => r.Ngay >= start && r.Ngay <= end);
                }

                // Project into DTO after Union
                var records = await query
                    .OrderBy(r => r.Ngay)
                    .Select(r => new AttendanceRecordDTO
                    {
                        Loai = r.Loai,
                        Ngay = r.Ngay.ToString("yyyy-MM-dd"),
                        GioVao = r.GioVao.HasValue ? r.GioVao.Value.ToString("HH:mm") : null,
                        GioRa = r.GioRa.HasValue ? r.GioRa.Value.ToString("HH:mm") : null,
                        TongGio = r.TongGio,
                        GhiChu = r.GhiChu,
                        TrangThai = r.TrangThai
                    })
                    .ToListAsync();

                return Ok(new { success = true, records });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [Route("GetPendingRecords")]
        public async Task<IActionResult> GetPendingRecords(string? startDate = null, string? endDate = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // Define the base queries with aligned anonymous types
                var chamCongQuery = _context.LichSuChamCongs
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "LS1")
                    .Select(c => new
                    {
                        Loai = "Chấm Công",
                        Ngay = c.Ngay, // Ensure DateOnly
                        GioVao = c.GioVao, // TimeOnly?
                        GioRa = c.GioRa, // TimeOnly?
                        TongGio = c.TongGio, // decimal?
                        GhiChu = c.GhiChu ?? "", // string
                        TrangThai = c.TrangThai // string
                    });

                var tangCaQuery = _context.TangCas
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "TC1")
                    .Select(c => new
                    {
                        Loai = "Tăng Ca",
                        Ngay = c.NgayTangCa, // Ensure DateOnly
                        GioVao = c.GioVao, // TimeOnly?
                        GioRa = c.GioRa, // TimeOnly?
                        TongGio = (decimal?)c.SoGioTangCa, // Cast to decimal? to align types
                        GhiChu = c.GhiChu ?? "", // string
                        TrangThai = c.TrangThai // string
                    });

                var lamBuQuery = _context.LamBus
                    .Where(c => c.MaNV == maNv.Value && c.TrangThai == "LB1")
                    .Select(c => new
                    {
                        Loai = "Làm Bù",
                        Ngay = c.NgayLamViec, // Ensure DateOnly
                        GioVao = c.GioVao, // TimeOnly?
                        GioRa = c.GioRa, // TimeOnly?
                        TongGio = c.TongGio, // decimal?
                        GhiChu = c.GhiChu ?? "", // string
                        TrangThai = c.TrangThai // string
                    });

                // Combine queries with Union
                var query = chamCongQuery
                    .Union(tangCaQuery)
                    .Union(lamBuQuery);

                // Apply date filtering if provided
                if (DateOnly.TryParse(startDate, out DateOnly start) && DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(r => r.Ngay >= start && r.Ngay <= end);
                }

                // Project into DTO after Union
                var records = await query
                    .OrderBy(r => r.Ngay)
                    .Select(r => new AttendanceRecordDTO
                    {
                        Loai = r.Loai,
                        Ngay = r.Ngay.ToString("yyyy-MM-dd"),
                        GioVao = r.GioVao.HasValue ? r.GioVao.Value.ToString("HH:mm") : null,
                        GioRa = r.GioRa.HasValue ? r.GioRa.Value.ToString("HH:mm") : null,
                        TongGio = r.TongGio,
                        GhiChu = r.GhiChu,
                        TrangThai = r.TrangThai
                    })
                    .ToListAsync();

                return Ok(new { success = true, records });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [Route("GetRejectedRecords")]
        public async Task<IActionResult> GetRejectedRecords(string? startDate = null, string? endDate = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // Define the base queries with aligned anonymous types
                var lichsuchamCongQuery = _context.LichSuChamCongs
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "LS4")
                    .Select(c => new
                    {
                        Loai = "Chấm Công lần 1",
                        Ngay = c.Ngay,
                        GioVao = c.GioVao,
                        GioRa = c.GioRa,
                        TongGio = c.TongGio,
                        GhiChu = c.GhiChu ?? "",
                        TrangThai = c.TrangThai
                    });

                var chamCongQuery = _context.ChamCongs
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "CC4")
                    .Select(c => new
                    {
                        Loai = "Chấm Công",
                        Ngay = c.NgayLamViec,
                        GioVao = c.GioVao,
                        GioRa = c.GioRa,
                        TongGio = c.TongGio,
                        GhiChu = c.GhiChu ?? "",
                        TrangThai = c.TrangThai
                    });

                var tangCaQuery = _context.TangCas
                    .Where(c => c.MaNv == maNv.Value && c.TrangThai == "TC4")
                    .Select(c => new
                    {
                        Loai = "Tăng Ca",
                        Ngay = c.NgayTangCa,
                        GioVao = c.GioVao,
                        GioRa = c.GioRa,
                        TongGio = (decimal?)c.SoGioTangCa,
                        GhiChu = c.GhiChu ?? "",
                        TrangThai = c.TrangThai
                    });

                var lamBuQuery = _context.LamBus
                    .Where(c => c.MaNV == maNv.Value && c.TrangThai == "LB4")
                    .Select(c => new
                    {
                        Loai = "Làm Bù",
                        Ngay = c.NgayLamViec,
                        GioVao = c.GioVao,
                        GioRa = c.GioRa,
                        TongGio = c.TongGio,
                        GhiChu = c.GhiChu ?? "",
                        TrangThai = c.TrangThai
                    });

                // Combine queries with Union
                var query = lichsuchamCongQuery
                    .Union(chamCongQuery)
                    .Union(tangCaQuery)
                    .Union(lamBuQuery);

                // Apply date filtering if provided
                if (DateOnly.TryParse(startDate, out DateOnly start) && DateOnly.TryParse(endDate, out DateOnly end))
                {
                    query = query.Where(r => r.Ngay >= start && r.Ngay <= end);
                }

                // Project into DTO after Union
                var records = await query
                    .OrderBy(r => r.Ngay)
                    .Select(r => new AttendanceRecordDTO
                    {
                        Loai = r.Loai,
                        Ngay = r.Ngay.ToString("yyyy-MM-dd"),
                        GioVao = r.GioVao.HasValue ? r.GioVao.Value.ToString("HH:mm") : null,
                        GioRa = r.GioRa.HasValue ? r.GioRa.Value.ToString("HH:mm") : null,
                        TongGio = r.TongGio,
                        GhiChu = r.GhiChu,
                        TrangThai = r.TrangThai
                    })
                    .ToListAsync();

                return Ok(new { success = true, records });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }
        [HttpDelete("DeleteRejectedRecord")]
        public async Task<IActionResult> DeleteRejectedRecord([FromBody] DeleteRecordRequest request)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                if (!DateOnly.TryParse(request.Ngay, out DateOnly ngay))
                {
                    return BadRequest(new { success = false, message = "Định dạng ngày không hợp lệ." });
                }

                int rowsAffected = 0;

                switch (request.Loai)
                {
                    case "Chấm Công lần 1":
                        var lichSuChamCong = await _context.LichSuChamCongs
                            .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.Ngay == ngay && c.TrangThai == "LS4");
                        if (lichSuChamCong != null)
                        {
                            _context.LichSuChamCongs.Remove(lichSuChamCong);
                            rowsAffected = await _context.SaveChangesAsync();
                        }
                        break;

                    case "Chấm Công":
                        var chamCong = await _context.ChamCongs
                            .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.NgayLamViec == ngay && c.TrangThai == "CC4");
                        if (chamCong != null)
                        {
                            _context.ChamCongs.Remove(chamCong);
                            rowsAffected = await _context.SaveChangesAsync();

                            // Kiểm tra và xóa bản ghi trong LichSuChamCong
                            var lichSuChamCongToDelete = await _context.LichSuChamCongs
                                .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.Ngay == ngay);
                            if (lichSuChamCongToDelete != null)
                            {
                                _context.LichSuChamCongs.Remove(lichSuChamCongToDelete);
                                rowsAffected += await _context.SaveChangesAsync();
                            }
                        }
                        break;

                    case "Tăng Ca":
                        var tangCa = await _context.TangCas
                            .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.NgayTangCa == ngay && c.TrangThai == "TC4");
                        if (tangCa != null)
                        {
                            _context.TangCas.Remove(tangCa);
                            rowsAffected = await _context.SaveChangesAsync();
                        }
                        break;

                    case "Làm Bù":
                        var lamBu = await _context.LamBus
                            .FirstOrDefaultAsync(c => c.MaNV == maNv.Value && c.NgayLamViec == ngay && c.TrangThai == "LB4");
                        if (lamBu != null)
                        {
                            _context.LamBus.Remove(lamBu);
                            rowsAffected = await _context.SaveChangesAsync();
                        }
                        break;

                    default:
                        return BadRequest(new { success = false, message = "Loại bản ghi không hợp lệ." });
                }

                if (rowsAffected > 0)
                {
                    return Ok(new { success = true, message = "Xóa bản ghi thành công." });
                }
                else
                {
                    return NotFound(new { success = false, message = "Không tìm thấy bản ghi để xóa." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        public class DeleteRecordRequest
        {
            public string Loai { get; set; }
            public string Ngay { get; set; }
        }

    }
}