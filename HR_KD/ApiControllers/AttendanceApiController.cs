//using HR_KD.Data;
//using HR_KD.DTOs;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace HR_KD.ApiControllers
//{
//    [Route("api/Attendance")]
//    [ApiController]
//    public class AttendanceController : ControllerBase
//    {
//        private readonly HrDbContext _context;

//        public AttendanceController(HrDbContext context)
//        {
//            _context = context;
//        }

//        private int? GetMaNvFromClaims()
//        {
//            var maNvClaim = User.FindFirst("MaNV")?.Value;
//            return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
//        }

//        // Helper method to get week range (Monday to Sunday)
//        private (DateOnly start, DateOnly end) GetWeekRange(DateOnly date)
//        {
//            DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);
//            int dayOfWeek = (int)dateTime.DayOfWeek;
//            int daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
//            DateOnly start = date.AddDays(-daysToMonday);
//            DateOnly end = start.AddDays(6);
//            return (start, end);
//        }

//        [HttpPost]
//        [Route("SubmitAttendance")]
//        public async Task<IActionResult> SubmitAttendance(AttendanceDataDTO data)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            if (data == null || (data.attendance == null || !data.attendance.Any()) && (data.overtime == null || !data.overtime.Any()))
//            {
//                return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
//            }

//            try
//            {
//                // Validate weekly hours and overtime for attendance
//                if (data.attendance != null && data.attendance.Any())
//                {
//                    foreach (var entry in data.attendance)
//                    {
//                        if (!DateOnly.TryParse(entry.NgayLamViec, out var ngayLamViec))
//                        {
//                            return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
//                        }

//                        // Check if already attended
//                        bool daChamCong = await _context.LichSuChamCongs.AnyAsync(c => c.MaNv == maNv.Value && c.Ngay == ngayLamViec);
//                        if (daChamCong)
//                        {
//                            return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã chấm công ngày {entry.NgayLamViec}." });
//                        }

//                        // Check if on leave
//                        bool daNghi = await _context.NgayNghis.AnyAsync(c => c.MaNv == maNv.Value && c.NgayNghi1 == ngayLamViec);
//                        if (daNghi)
//                        {
//                            return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã nghỉ ngày {entry.NgayLamViec}.", error = "Employee on leave" });
//                        }

//                        // Check weekly working hours (max 48 hours)
//                        var (start, end) = GetWeekRange(ngayLamViec);
//                        var weeklyHours = await _context.LichSuChamCongs
//                            .Where(c => c.MaNv == maNv.Value && c.Ngay >= start && c.Ngay <= end)
//                            .SumAsync(c => c.TongGio ?? 0);
//                        if (weeklyHours >= 48)
//                        {
//                            return BadRequest(new { success = false, message = $"Đã đủ 48 giờ làm việc trong tuần bắt đầu từ {start}, không thể chấm công thêm." });
//                        }

//                        var chamCong = new LichSuChamCong
//                        {
//                            MaNv = maNv.Value,
//                            Ngay = ngayLamViec,
//                            GioVao = TimeOnly.TryParse(entry.GioVao, out var parsedGioVao) ? parsedGioVao : null,
//                            GioRa = TimeOnly.TryParse(entry.GioRa, out var parsedGioRa) ? parsedGioRa : null,
//                            TongGio = entry.TongGio ?? 0,
//                            TrangThai = entry.TrangThai,
//                            GhiChu = entry.GhiChu
//                        };
//                        _context.LichSuChamCongs.Add(chamCong);
//                    }
//                    await _context.SaveChangesAsync();
//                }

//                // Validate weekly overtime hours
//                if (data.overtime != null && data.overtime.Any())
//                {
//                    foreach (var entry in data.overtime)
//                    {
//                        if (!DateOnly.TryParse(entry.NgayTangCa, out var ngayTangCa))
//                        {
//                            return BadRequest(new { success = false, message = $"Ngày tăng ca không hợp lệ: {entry.NgayTangCa}" });
//                        }

//                        // Check weekly overtime hours (max 12 hours)
//                        var (start, end) = GetWeekRange(ngayTangCa);
//                        var weeklyOvertimeHours = await _context.TangCas
//                            .Where(c => c.MaNv == maNv.Value && c.NgayTangCa >= start && c.NgayTangCa <= end)
//                            .SumAsync(c => c.SoGioTangCa);
//                        if (weeklyOvertimeHours >= 12)
//                        {
//                            return BadRequest(new { success = false, message = $"Đã đủ 12 giờ tăng ca trong tuần bắt đầu từ {start}, không thể tăng ca thêm." });
//                        }

//                        var tangCa = new TangCa
//                        {
//                            MaNv = maNv.Value,
//                            NgayTangCa = ngayTangCa,
//                            SoGioTangCa = entry.SoGioTangCa,
//                            TyLeTangCa = 1, // Default overtime rate
//                            TrangThai = "Chờ duyệt"
//                        };
//                        _context.TangCas.Add(tangCa);
//                    }
//                    await _context.SaveChangesAsync();
//                }

//                return Ok(new { success = true, message = "Chấm công thành công." });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "Lỗi hệ thống.",
//                    error = ex.Message,
//                    stackTrace = ex.StackTrace
//                });
//            }
//        }

//        // New API to get overtime records
//        [HttpGet]
//        [Route("GetOvertimeRecords")]
//        public async Task<IActionResult> GetOvertimeRecords(string? ngayTangCa = null)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            var query = _context.TangCas.Where(c => c.MaNv == maNv.Value);

//            if (!string.IsNullOrEmpty(ngayTangCa) && DateOnly.TryParse(ngayTangCa, out DateOnly ngay))
//            {
//                query = query.Where(c => c.NgayTangCa == ngay);
//            }

//            var records = await query
//                .Select(c => new
//                {
//                    c.NgayTangCa,
//                    c.SoGioTangCa,
//                    c.TrangThai
//                })
//                .ToListAsync();

//            return Ok(new { success = true, records });
//        }

//        // Existing APIs (unchanged except for minor adjustments)
//        [HttpGet]
//        [Route("GetAttendanceRecords")]
//        public async Task<IActionResult> GetAttendanceRecords(string? ngayLamViec = null)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            var query = _context.ChamCongs.Where(c => c.MaNv == maNv.Value && c.TrangThai == "Đã duyệt lận 1");

//            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
//            {
//                query = query.Where(c => c.NgayLamViec == ngay);
//            }

//            var records = await query
//                .Select(c => new
//                {
//                    c.NgayLamViec,
//                    GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString("HH:mm") : null,
//                    GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString("HH:mm") : null,
//                    c.TongGio,
//                    c.TrangThai,
//                    c.GhiChu
//                })
//                .ToListAsync();

//            return Ok(new { success = true, records });
//        }

//        [HttpGet]
//        [Route("GetAttendanceHistoryRecords")]
//        public async Task<IActionResult> GetAttendanceHistoryRecords(string? ngayLamViec = null)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            var query = _context.LichSuChamCongs.Where(c => c.MaNv == maNv.Value);

//            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
//            {
//                query = query.Where(c => c.Ngay == ngay);
//            }

//            var records = await query
//                .Select(c => new
//                {
//                    c.Ngay,
//                    GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString("HH:mm") : null,
//                    GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString("HH:mm") : null,
//                    c.TongGio,
//                    c.TrangThai,
//                    c.GhiChu
//                })
//                .ToListAsync();

//            return Ok(new { success = true, records });
//        }

//        [HttpGet]
//        [Route("GetAttendanceHistoryRecords2")]
//        public async Task<IActionResult> GetAttendanceHistoryRecords2(string? ngayLamViec = null)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            var query = _context.LichSuChamCongs.Where(c => c.MaNv == maNv.Value && c.TrangThai == "Chờ duyệt");

//            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
//            {
//                query = query.Where(c => c.Ngay == ngay);
//            }

//            var records = await query
//                .Select(c => new
//                {
//                    c.Ngay,
//                    GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString("HH:mm") : null,
//                    GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString("HH:mm") : null,
//                    c.TongGio,
//                    c.TrangThai,
//                    c.GhiChu
//                })
//                .ToListAsync();

//            return Ok(new { success = true, records });
//        }

//        [HttpGet("GetAttendanceRequests")]
//        public async Task<IActionResult> GetAttendanceRequests(string? ngayLamViec = null)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            var query = _context.YeuCauSuaChamCongs.Where(c => c.MaNv == maNv.Value);

//            if (!string.IsNullOrEmpty(ngayLamViec) && DateOnly.TryParse(ngayLamViec, out DateOnly ngay))
//            {
//                query = query.Where(c => c.NgayLamViec == ngay);
//            }

//            var requests = await query
//                .Select(c => new
//                {
//                    c.MaYeuCau,
//                    c.NgayLamViec,
//                    GioVaoMoi = c.GioVaoMoi.HasValue ? c.GioVaoMoi.Value.ToString("HH:mm") : null,
//                    GioRaMoi = c.GioRaMoi.HasValue ? c.GioRaMoi.Value.ToString("HH:mm") : null,
//                    c.LyDo,
//                    c.TrangThai
//                })
//                .ToListAsync();

//            return Ok(new { success = true, requests });
//        }

//        [HttpPost("AcceptAttendanceRequest")]
//        public async Task<IActionResult> AcceptAttendanceRequest(AcceptAttendanceRequestDTO request)
//        {
//            var maNv = GetMaNvFromClaims();
//            if (!maNv.HasValue)
//            {
//                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
//            }

//            var yeuCau = await _context.YeuCauSuaChamCongs
//                .FirstOrDefaultAsync(y => y.MaYeuCau == request.MaYeuCau && y.MaNv == maNv.Value);

//            if (yeuCau == null)
//            {
//                return NotFound(new { success = false, message = "Không tìm thấy yêu cầu chấm công." });
//            }

//            try
//            {
//                foreach (var entry in request.AttendanceData)
//                {
//                    if (!DateOnly.TryParse(entry.NgayLamViec, out var ngayLamViec))
//                    {
//                        return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
//                    }

//                    var chamCong = await _context.ChamCongs
//                        .FirstOrDefaultAsync(c => c.MaNv == maNv.Value && c.NgayLamViec == ngayLamViec);

//                    if (chamCong == null)
//                    {
//                        chamCong = new ChamCong
//                        {
//                            MaNv = maNv.Value,
//                            NgayLamViec = ngayLamViec,
//                            TrangThai = "Đã duyệt lận 1",
//                            GhiChu = "Chấp nhận yêu cầu chấm công"
//                        };
//                        _context.ChamCongs.Add(chamCong);
//                    }

//                    chamCong.GioVao = yeuCau.GioVaoMoi ?? chamCong.GioVao;
//                    chamCong.GioRa = yeuCau.GioRaMoi ?? chamCong.GioRa;

//                    if (chamCong.GioVao.HasValue && chamCong.GioRa.HasValue)
//                    {
//                        chamCong.TongGio = (decimal)(chamCong.GioRa.Value - chamCong.GioVao.Value).TotalHours;
//                    }
//                }

//                _context.YeuCauSuaChamCongs.Remove(yeuCau);
//                await _context.SaveChangesAsync();

//                return Ok(new
//                {
//                    success = true,
//                    message = "Xử lý yêu cầu thành công",
//                    deletedRequestId = yeuCau.MaYeuCau
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "Lỗi hệ thống",
//                    error = ex.Message
//                });
//            }
//        }

//        [HttpGet("GetStatusForMonth")]
//        public async Task<IActionResult> GetStatusForMonth(int maNV, string? monthYear = null)
//        {
//            DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
//                ? DateTime.Now
//                : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
//                    ? parsedMonth
//                    : DateTime.Now;

//            var records = await _context.ChamCongs
//                .Where(c => c.MaNv == maNV &&
//                            c.NgayLamViec.Year == selectedMonth.Year &&
//                            c.NgayLamViec.Month == selectedMonth.Month &&
//                            c.TrangThai.Trim() == "Đã duyệt lận 1")
//                .ToListAsync();

//            if (!records.Any())
//            {
//                var historyRecords = await _context.LichSuChamCongs
//                    .Where(c => c.MaNv == maNV &&
//                                c.Ngay.Year == selectedMonth.Year &&
//                                c.Ngay.Month == selectedMonth.Month &&
//                                c.TrangThai.Trim() == "Đã duyệt lận 1")
//                    .ToListAsync();

//                if (!historyRecords.Any())
//                {
//                    return Ok(new { status = "Chưa có dữ liệu chấm công đã duyệt" });
//                }

//                var historyApprovedCount = historyRecords.Count;
//                var historyTotalDays = historyRecords.Count;
//                return Ok(new { status = $"Lịch sử: {historyApprovedCount}/{historyTotalDays} ngày đã duyệt" });
//            }

//            var approvedCount = records.Count;
//            var totalDays = records.Count;

//            return Ok(new { status = $"{approvedCount}/{totalDays} ngày đã duyệt" });
//        }

//        [HttpPost("RejectAttendanceRequest")]
//        public async Task<IActionResult> RejectAttendanceRequest(RejectAttendanceRequestDTO request)
//        {
//            try
//            {
//                var requestRecord = await _context.YeuCauSuaChamCongs
//                    .FirstOrDefaultAsync(a => a.MaYeuCau == request.MaYeuCau);

//                if (requestRecord == null)
//                {
//                    return NotFound(new { success = false, message = "Không tìm thấy yêu cầu chấm công." });
//                }

//                if (request.NgayLamViec == default(DateOnly) || request.NgayLamViec == DateOnly.MinValue)
//                {
//                    return BadRequest(new { success = false, message = "Ngày làm việc không hợp lệ." });
//                }

//                var ngayNghi = new NgayNghi
//                {
//                    MaNv = requestRecord.MaNv,
//                    NgayNghi1 = request.NgayLamViec,
//                    LyDo = !string.IsNullOrEmpty(request.LyDo) ? request.LyDo : "Không có lý do cụ thể",
//                    MaLoaiNgayNghi = request.MaLoaiNgayNghi,
//                    MaTrangThai = 3,
//                    NgayLamDon = DateTime.Now
//                };

//                _context.NgayNghis.Add(ngayNghi);
//                await _context.SaveChangesAsync();

//                _context.YeuCauSuaChamCongs.Remove(requestRecord);
//                await _context.SaveChangesAsync();

//                return Ok(new { success = true, message = "Yêu cầu chấm công đã bị từ chối và lưu vào NgayNghi." });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
//            }
//        }
//    }
//}