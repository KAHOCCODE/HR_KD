
using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    [Route("api/AttendanceReportApi")]
    [ApiController]
    public class AttendanceReportApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public AttendanceReportApiController(HrDbContext context)
        {
            _context = context;
        }

        private int? GetMaNvFromClaims()
        {
            var maNvClaim = User.FindFirst("MaNV")?.Value;
            return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
        }

        [HttpGet("GetMonthlyReport")]
        public async Task<IActionResult> GetMonthlyReport(string? monthYear = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // Parse the month and year, default to current month if not provided
                DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                    ? DateTime.Now
                    : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                        ? parsedMonth
                        : DateTime.Now;

                DateOnly startDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
                DateOnly endDate = startDate.AddMonths(1).AddDays(-1);

                // Fetch attendance records with statuses CC3, CC4, CC5, CC6
                var attendanceRecords = await _context.ChamCongs
                    .Where(c => c.MaNv == maNv.Value &&
                                c.NgayLamViec >= startDate &&
                                c.NgayLamViec <= endDate &&
                                (c.TrangThai == "CC3" || c.TrangThai == "CC4" || c.TrangThai == "CC5" || c.TrangThai == "CC6"))
                    .Join(_context.TrangThais,
                          c => c.TrangThai,
                          t => t.MaTrangThai,
                          (c, t) => new
                          {
                              c.NgayLamViec,
                              TrangThai = t.TenTrangThai,
                              TongGio = (decimal?)c.TongGio,
                              GioVao = c.GioVao.HasValue ? c.GioVao.Value.ToString("HH:mm") : null,
                              GioRa = c.GioRa.HasValue ? c.GioRa.Value.ToString("HH:mm") : null,
                              c.GhiChu
                          })
                    .ToListAsync();

                // Fetch leave days
                var leaveDays = await _context.NgayNghis
                    .Where(n => n.MaNv == maNv.Value &&
                                n.NgayNghi1 >= startDate &&
                                n.NgayNghi1 <= endDate)
                    .Select(n => new
                    {
                        n.NgayNghi1,
                        n.LyDo
                    })
                    .ToListAsync();

                // Fetch overtime records with status TC3
                var overtimeRecords = await _context.TangCas
                    .Where(t => t.MaNv == maNv.Value &&
                                t.NgayTangCa >= startDate &&
                                t.NgayTangCa <= endDate &&
                                t.TrangThai == "TC3")
                    .Join(_context.TrangThais,
                          t => t.TrangThai,
                          s => s.MaTrangThai,
                          (t, s) => new
                          {
                              t.NgayTangCa,
                              TrangThai = s.TenTrangThai,
                              TongGio = (decimal?)t.SoGioTangCa,
                              GioVao = t.GioVao.HasValue ? t.GioVao.Value.ToString("HH:mm") : null,
                              GioRa = t.GioRa.HasValue ? t.GioRa.Value.ToString("HH:mm") : null,
                              t.GhiChu
                          })
                    .ToListAsync();

                // Fetch missing hours
                var missingHoursRecords = await _context.GioThieus
                    .Where(g => g.MaNv == maNv.Value &&
                                g.NgayThieu >= startDate &&
                                g.NgayThieu <= endDate)
                    .Select(g => new
                    {
                        g.NgayThieu,
                        g.TongGioThieu
                    })
                    .ToListAsync();

                // Calculate total working days in the month (excluding weekends)
                int totalDays = endDate.Day;
                int workingDays = 0;
                for (int day = 1; day <= totalDays; day++)
                {
                    var date = new DateOnly(selectedMonth.Year, selectedMonth.Month, day);
                    var dateTime = date.ToDateTime(TimeOnly.MinValue);
                    if (dateTime.DayOfWeek != DayOfWeek.Saturday && dateTime.DayOfWeek != DayOfWeek.Sunday)
                    {
                        workingDays++;
                    }
                }

                // Calculate statistics
                var attendanceDays = attendanceRecords.Count;
                var leaveDaysCount = leaveDays.Count;
                var missingDays = workingDays - attendanceDays - leaveDaysCount;
                var totalAttendanceHours = attendanceRecords.Sum(r => r.TongGio ?? 0);
                var totalMissingHours = missingHoursRecords.Sum(r => r.TongGioThieu);
                var totalOvertimeHours = overtimeRecords.Sum(r => r.TongGio ?? 0);

                // Map attendance records to calendar events
                var calendarEvents = attendanceRecords.Select(r => new
                {
                    Date = r.NgayLamViec.ToString("yyyy-MM-dd"),
                    Status = r.TrangThai,
                    Color = r.TrangThai.ToLower().Contains("đã duyệt") ? "#28a745" : // Green for Approved
                           r.TrangThai.ToLower().Contains("từ chối") ? "#dc3545" : // Red for Rejected
                           r.TrangThai.ToLower().Contains("chờ duyệt") ? "#ffc107" : // Yellow for Pending
                           "#17a2b8", // Cyan for Other
                    Details = new
                    {
                        TongGio = r.TongGio,
                        GioVao = r.GioVao,
                        GioRa = r.GioRa,
                        GhiChu = r.GhiChu ?? ""
                    }
                }).ToList();

                // Map leave days to calendar events
                var leaveEvents = leaveDays.Select(l => new
                {
                    Date = l.NgayNghi1.ToString("yyyy-MM-dd"),
                    Status = "Nghỉ phép",
                    Color = "#6c757d", // Gray for leave
                    Details = new
                    {
                        TongGio = (decimal?)0,
                        GioVao = (string)null,
                        GioRa = (string)null,
                        GhiChu = l.LyDo ?? ""
                    }
                }).ToList();

                // Map overtime records to calendar events
                var overtimeEvents = overtimeRecords.Select(t => new
                {
                    Date = t.NgayTangCa.ToString("yyyy-MM-dd"),
                    Status = t.TrangThai,
                    Color = "#007bff", // Blue for overtime
                    Details = new
                    {
                        TongGio = t.TongGio,
                        GioVao = t.GioVao,
                        GioRa = t.GioRa,
                        GhiChu = t.GhiChu ?? ""
                    }
                }).ToList();

                // Combine events
                var allEvents = calendarEvents
                    .Concat(leaveEvents)
                    .Concat(overtimeEvents)
                    .OrderBy(e => e.Date)
                    .ToList();

                // Prepare response
                var response = new
                {
                    success = true,
                    summary = new
                    {
                        AttendanceDays = attendanceDays,
                        LeaveDays = leaveDaysCount,
                        MissingDays = missingDays,
                        TotalWorkingDays = workingDays,
                        OvertimeDays = overtimeRecords.Count,
                        TotalAttendanceHours = totalAttendanceHours,
                        TotalMissingHours = totalMissingHours,
                        TotalOvertimeHours = totalOvertimeHours
                    },
                    calendar = allEvents
                };

                return Ok(response);
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
    }
}