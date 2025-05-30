﻿
using ClosedXML.Excel;
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
        private string GetRoleFromClaims()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        }

        [HttpGet("GetDepartmentsManager")]
        public IActionResult GetDepartments()
        {
            var departments = _context.PhongBans
                .Select(pb => new { pb.MaPhongBan, pb.TenPhongBan })
                .ToList();

            return Ok(departments);
        }

        [HttpGet("GetDepartmentsManagerIndex")]
        public IActionResult GetDepartmentsIndex()
        {
            int? maNv = GetMaNvFromClaims();
            if (maNv == null)
                return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

            var departments = _context.NhanViens
                .Where(nv => nv.MaNv == maNv)
                .Select(nv => new
                {
                    nv.MaPhongBanNavigation.MaPhongBan,
                    nv.MaPhongBanNavigation.TenPhongBan
                })
                .FirstOrDefault();

            if (departments == null)
                return NotFound("Không tìm thấy phòng ban của nhân viên.");

            return Ok(departments);
        }

        [HttpGet("GetPositionsManager")]
        public IActionResult GetPositions()
        {
            var positions = _context.ChucVus
                .Select(cv => new { cv.MaChucVu, cv.TenChucVu })
                .ToList();

            return Ok(positions);
        }

        [HttpGet("GetEmployeesManager")]
        public IActionResult GetEmployees(int? maPhongBan, int? maChucVu)
        {
            var employees = _context.NhanViens.AsQueryable();

            if (maPhongBan.HasValue)
            {
                employees = employees.Where(nv => nv.MaPhongBan == maPhongBan.Value);
            }

            if (maChucVu.HasValue)
            {
                employees = employees.Where(nv => nv.MaChucVu == maChucVu.Value);
            }

            var result = employees
                .Select(nv => new { nv.MaNv, nv.HoTen })
                .ToList();

            return Ok(result);
        }

        [HttpGet("GetTrangThai")]
        public IActionResult GetTrangThai()
        {
            var trangThai = _context.TrangThais
                .Select(tt => new { tt.MaTrangThai, tt.TenTrangThai })
                .ToList();

            return Ok(trangThai);
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
                DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                    ? DateTime.Now
                    : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                        ? parsedMonth
                        : DateTime.Now;

                DateOnly startDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
                DateOnly endDate = startDate.AddMonths(1).AddDays(-1);

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

                var attendanceDays = attendanceRecords.Count;
                var leaveDaysCount = leaveDays.Count;
                var missingDays = workingDays - attendanceDays - leaveDaysCount;
                var totalAttendanceHours = attendanceRecords.Sum(r => r.TongGio ?? 0);
                var totalMissingHours = missingHoursRecords.Sum(r => r.TongGioThieu);
                var totalOvertimeHours = overtimeRecords.Sum(r => r.TongGio ?? 0);

                var calendarEvents = attendanceRecords.Select(r => new
                {
                    Date = r.NgayLamViec.ToString("yyyy-MM-dd"),
                    Status = r.TrangThai,
                    Color = r.TrangThai.ToLower().Contains("đã duyệt") ? "#28a745" :
                           r.TrangThai.ToLower().Contains("từ chối") ? "#dc3545" :
                           r.TrangThai.ToLower().Contains("chờ duyệt") ? "#ffc107" :
                           "#17a2b8",
                    Details = new
                    {
                        TongGio = r.TongGio,
                        GioVao = r.GioVao,
                        GioRa = r.GioRa,
                        GhiChu = r.GhiChu ?? ""
                    }
                }).ToList();

                var leaveEvents = leaveDays.Select(l => new
                {
                    Date = l.NgayNghi1.ToString("yyyy-MM-dd"),
                    Status = "Nghỉ phép",
                    Color = "#6c757d",
                    Details = new
                    {
                        TongGio = (decimal?)0,
                        GioVao = (string)null,
                        GioRa = (string)null,
                        GhiChu = l.LyDo ?? ""
                    }
                }).ToList();

                var overtimeEvents = overtimeRecords.Select(t => new
                {
                    Date = t.NgayTangCa.ToString("yyyy-MM-dd"),
                    Status = t.TrangThai,
                    Color = "#007bff",
                    Details = new
                    {
                        TongGio = t.TongGio,
                        GioVao = t.GioVao,
                        GioRa = t.GioRa,
                        GhiChu = t.GhiChu ?? ""
                    }
                }).ToList();

                var allEvents = calendarEvents
                    .Concat(leaveEvents)
                    .Concat(overtimeEvents)
                    .OrderBy(e => e.Date)
                    .ToList();

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

        [HttpGet("GetAllEmployeesMonthlyReport")]
        public async Task<IActionResult> GetAllEmployeesMonthlyReport(string? monthYear = null, string? year = null, int? maPhongBan = null, int? maChucVu = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                    ? DateTime.Now
                    : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                        ? parsedMonth
                        : DateTime.Now;

                int selectedYear = string.IsNullOrEmpty(year)
                    ? DateTime.Now.Year
                    : int.TryParse(year, out int parsedYear)
                        ? parsedYear
                        : DateTime.Now.Year;

                DateOnly startDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
                DateOnly endDate = startDate.AddMonths(1).AddDays(-1);

                var employeesQuery = _context.NhanViens.AsQueryable();
                if (maPhongBan.HasValue)
                {
                    employeesQuery = employeesQuery.Where(nv => nv.MaPhongBan == maPhongBan.Value);
                }
                if (maChucVu.HasValue)
                {
                    employeesQuery = employeesQuery.Where(nv => nv.MaChucVu == maChucVu.Value);
                }

                var employees = await employeesQuery
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.HoTen,
                        TenPhongBan = nv.MaPhongBanNavigation.TenPhongBan,
                        TenChucVu = nv.MaChucVuNavigation.TenChucVu
                    })
                    .ToListAsync();

                var employeeReports = new List<object>();
                foreach (var emp in employees)
                {
                    var attendanceRecords = await _context.ChamCongs
                        .Where(c => c.MaNv == emp.MaNv &&
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

                    var leaveDays = await _context.NgayNghis
                        .Where(n => n.MaNv == emp.MaNv &&
                                    n.NgayNghi1 >= startDate &&
                                    n.NgayNghi1 <= endDate)
                        .Select(n => new
                        {
                            n.NgayNghi1,
                            n.LyDo
                        })
                        .ToListAsync();

                    var overtimeRecords = await _context.TangCas
                        .Where(t => t.MaNv == emp.MaNv &&
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

                    var missingHoursRecords = await _context.GioThieus
                        .Where(g => g.MaNv == emp.MaNv &&
                                    g.NgayThieu >= startDate &&
                                    g.NgayThieu <= endDate)
                        .Select(g => new
                        {
                            g.NgayThieu,
                            g.TongGioThieu
                        })
                        .ToListAsync();

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

                    var attendanceDays = attendanceRecords.Count;
                    var leaveDaysCount = leaveDays.Count;
                    var missingDays = workingDays - attendanceDays - leaveDaysCount;
                    var totalAttendanceHours = attendanceRecords.Sum(r => r.TongGio ?? 0);
                    var totalMissingHours = missingHoursRecords.Sum(r => r.TongGioThieu);
                    var totalOvertimeHours = overtimeRecords.Sum(r => r.TongGio ?? 0);

                    employeeReports.Add(new
                    {
                        maNv = emp.MaNv,
                        hoTen = emp.HoTen,
                        tenPhongBan = emp.TenPhongBan,
                        tenChucVu = emp.TenChucVu,
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
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    employees = employeeReports
                });
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
        [HttpGet("ExportMonthlyReport")]
        public async Task<IActionResult> ExportMonthlyReport(string? monthYear = null, int? maPhongBan = null, int? maChucVu = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // Parse month and year
                DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                    ? DateTime.Now
                    : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                        ? parsedMonth
                        : DateTime.Now;

                DateOnly startDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
                DateOnly endDate = startDate.AddMonths(1).AddDays(-1);

                // Fetch employees based on filters
                var employeesQuery = _context.NhanViens.AsQueryable();
                if (maPhongBan.HasValue)
                {
                    employeesQuery = employeesQuery.Where(nv => nv.MaPhongBan == maPhongBan.Value);
                }
                if (maChucVu.HasValue)
                {
                    employeesQuery = employeesQuery.Where(nv => nv.MaChucVu == maChucVu.Value);
                }

                var employees = await employeesQuery
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.HoTen,
                        TenPhongBan = nv.MaPhongBanNavigation.TenPhongBan,
                        TenChucVu = nv.MaChucVuNavigation.TenChucVu
                    })
                    .ToListAsync();

                // Create Excel workbook
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Báo Cáo Chấm Công");

                // Add title
                worksheet.Cell(1, 1).Value = $"Báo Cáo Chấm Công Tháng {selectedMonth:yyyy-MM}";
                worksheet.Range("A1:L1").Merge();
                worksheet.Cell(1, 1).Style
                    .Font.SetBold()
                    .Font.SetFontSize(16)
                    .Font.SetFontColor(XLColor.Black)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(200, 220, 255));

                // Add a blank row
                worksheet.Row(2).Height = 10;

                // Set headers
                worksheet.Cell(3, 1).Value = "Mã NV";
                worksheet.Cell(3, 2).Value = "Họ Tên";
                worksheet.Cell(3, 3).Value = "Phòng Ban";
                worksheet.Cell(3, 4).Value = "Chức Vụ";
                worksheet.Cell(3, 5).Value = "Ngày Chấm Công";
                worksheet.Cell(3, 6).Value = "Ngày Nghỉ Phép";
                worksheet.Cell(3, 7).Value = "Ngày Thiếu";
                worksheet.Cell(3, 8).Value = "Tổng Ngày Làm";
                worksheet.Cell(3, 9).Value = "Ngày Tăng Ca";
                worksheet.Cell(3, 10).Value = "Tổng Giờ Chấm Công";
                worksheet.Cell(3, 11).Value = "Tổng Giờ Thiếu";
                worksheet.Cell(3, 12).Value = "Tổng Giờ Tăng Ca";

                // Style headers
                var headerRange = worksheet.Range("A3:L3");
                headerRange.Style
                    .Font.SetBold()
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(0, 102, 204))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetLeftBorderColor(XLColor.Black);

                // Populate data
                int row = 4;
                foreach (var emp in employees)
                {
                    var attendanceRecords = await _context.ChamCongs
                        .Where(c => c.MaNv == emp.MaNv &&
                                    c.NgayLamViec >= startDate &&
                                    c.NgayLamViec <= endDate &&
                                    (c.TrangThai == "CC3" || c.TrangThai == "CC4" || c.TrangThai == "CC5" || c.TrangThai == "CC6"))
                        .ToListAsync();

                    var leaveDays = await _context.NgayNghis
                        .Where(n => n.MaNv == emp.MaNv &&
                                    n.NgayNghi1 >= startDate &&
                                    n.NgayNghi1 <= endDate)
                        .ToListAsync();

                    var overtimeRecords = await _context.TangCas
                        .Where(t => t.MaNv == emp.MaNv &&
                                    t.NgayTangCa >= startDate &&
                                    t.NgayTangCa <= endDate &&
                                    t.TrangThai == "TC3")
                        .ToListAsync();

                    var missingHoursRecords = await _context.GioThieus
                        .Where(g => g.MaNv == emp.MaNv &&
                                    g.NgayThieu >= startDate &&
                                    g.NgayThieu <= endDate)
                        .ToListAsync();

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

                    var attendanceDays = attendanceRecords.Count;
                    var leaveDaysCount = leaveDays.Count;
                    var missingDays = workingDays - attendanceDays - leaveDaysCount;
                    var totalAttendanceHours = attendanceRecords.Sum(r => r.TongGio ?? 0m);
                    var totalMissingHours = missingHoursRecords.Sum(r => r.TongGioThieu );
                    var totalOvertimeHours = overtimeRecords.Sum(r => r.SoGioTangCa );

                    worksheet.Cell(row, 1).Value = emp.MaNv;
                    worksheet.Cell(row, 2).Value = emp.HoTen;
                    worksheet.Cell(row, 3).Value = emp.TenPhongBan;
                    worksheet.Cell(row, 4).Value = emp.TenChucVu;
                    worksheet.Cell(row, 5).Value = attendanceDays;
                    worksheet.Cell(row, 6).Value = leaveDaysCount;
                    worksheet.Cell(row, 7).Value = missingDays;
                    worksheet.Cell(row, 8).Value = workingDays;
                    worksheet.Cell(row, 9).Value = overtimeRecords.Count;
                    worksheet.Cell(row, 10).Value = totalAttendanceHours;
                    worksheet.Cell(row, 11).Value = totalMissingHours;
                    worksheet.Cell(row, 12).Value = totalOvertimeHours;

                    // Style data row
                    var dataRange = worksheet.Range($"A{row}:L{row}");
                    dataRange.Style
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetLeftBorderColor(XLColor.Black)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                    // Format numeric columns to 2 decimal places
                    worksheet.Cell(row, 10).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(row, 11).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(row, 12).Style.NumberFormat.Format = "0.00";

                    row++;
                }

                // Auto-size columns with a minimum and maximum width
                foreach (var column in worksheet.Columns(1, 12))
                {
                    column.AdjustToContents();
                    if (column.Width < 10)
                        column.Width = 10;
                    if (column.Width > 30)
                        column.Width = 30;
                }

                // Adjust row heights for better readability
                worksheet.Rows(3, row - 1).Height = 20;

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                // Generate file name
                string fileName = $"BaoCaoChamCong_{selectedMonth:yyyy-MM}.xlsx";

                // Return file
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống khi xuất Excel.",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpGet("GetManagerDepartmentReport")]
        public async Task<IActionResult> GetManagerDepartmentReport(string? monthYear = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // Get the department of the logged-in employee
                var managerDepartment = await _context.NhanViens
                    .Where(nv => nv.MaNv == maNv.Value)
                    .Select(nv => nv.MaPhongBan)
                    .FirstOrDefaultAsync();

                // Check if department exists
                if (managerDepartment == 0) // Assuming 0 indicates an invalid or unassigned department
                {
                    return NotFound(new { success = false, message = "Không tìm thấy phòng ban của nhân viên." });
                }

                // Parse month and year
                DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                    ? DateTime.Now
                    : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                        ? parsedMonth
                        : DateTime.Now;

                DateOnly startDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
                DateOnly endDate = startDate.AddMonths(1).AddDays(-1);

                // Fetch employees in the manager's department
                var employees = await _context.NhanViens
                    .Where(nv => nv.MaPhongBan == managerDepartment)
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.HoTen,
                        TenPhongBan = nv.MaPhongBanNavigation.TenPhongBan,
                        TenChucVu = nv.MaChucVuNavigation.TenChucVu
                    })
                    .ToListAsync();

                var employeeReports = new List<object>();
                foreach (var emp in employees)
                {
                    var attendanceRecords = await _context.ChamCongs
                        .Where(c => c.MaNv == emp.MaNv &&
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

                    var leaveDays = await _context.NgayNghis
                        .Where(n => n.MaNv == emp.MaNv &&
                                    n.NgayNghi1 >= startDate &&
                                    n.NgayNghi1 <= endDate)
                        .Select(n => new
                        {
                            n.NgayNghi1,
                            n.LyDo
                        })
                        .ToListAsync();

                    var overtimeRecords = await _context.TangCas
                        .Where(t => t.MaNv == emp.MaNv &&
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

                    var missingHoursRecords = await _context.GioThieus
                        .Where(g => g.MaNv == emp.MaNv &&
                                    g.NgayThieu >= startDate &&
                                    g.NgayThieu <= endDate)
                        .Select(g => new
                        {
                            g.NgayThieu,
                            g.TongGioThieu
                        })
                        .ToListAsync();

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

                    var attendanceDays = attendanceRecords.Count;
                    var leaveDaysCount = leaveDays.Count;
                    var missingDays = workingDays - attendanceDays - leaveDaysCount;
                    var totalAttendanceHours = attendanceRecords.Sum(r => r.TongGio ?? 0);
                    var totalMissingHours = missingHoursRecords.Sum(r => r.TongGioThieu);
                    var totalOvertimeHours = overtimeRecords.Sum(r => r.TongGio ?? 0);

                    employeeReports.Add(new
                    {
                        maNv = emp.MaNv,
                        hoTen = emp.HoTen,
                        tenPhongBan = emp.TenPhongBan,
                        tenChucVu = emp.TenChucVu,
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
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    employees = employeeReports
                });
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
        [HttpGet("ExportMonthlyManagerReport")]
        public async Task<IActionResult> ExportMonthlyManagerReport(string? monthYear = null, int? maPhongBan = null, int? maChucVu = null)
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
            }

            try
            {
                // If maPhongBan is not provided, fetch it from the manager's department
                if (!maPhongBan.HasValue)
                {
                    maPhongBan = await _context.NhanViens
                        .Where(nv => nv.MaNv == maNv.Value)
                        .Select(nv => nv.MaPhongBan)
                        .FirstOrDefaultAsync();

                    if (maPhongBan == 0)
                    {
                        return NotFound(new { success = false, message = "Không tìm thấy phòng ban của nhân viên." });
                    }
                }

                // Parse month and year
                DateTime selectedMonth = string.IsNullOrEmpty(monthYear)
                    ? DateTime.Now
                    : DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime parsedMonth)
                        ? parsedMonth
                        : DateTime.Now;

                DateOnly startDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
                DateOnly endDate = startDate.AddMonths(1).AddDays(-1);

                // Fetch employees based on filters
                var employeesQuery = _context.NhanViens.AsQueryable();
                employeesQuery = employeesQuery.Where(nv => nv.MaPhongBan == maPhongBan.Value);
                if (maChucVu.HasValue)
                {
                    employeesQuery = employeesQuery.Where(nv => nv.MaChucVu == maChucVu.Value);
                }

                var employees = await employeesQuery
                    .Select(nv => new
                    {
                        nv.MaNv,
                        nv.HoTen,
                        TenPhongBan = nv.MaPhongBanNavigation.TenPhongBan,
                        TenChucVu = nv.MaChucVuNavigation.TenChucVu
                    })
                    .ToListAsync();

                // Create Excel workbook
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Báo Cáo Chấm Công");

                // Add title
                worksheet.Cell(1, 1).Value = $"Báo Cáo Chấm Công Tháng {selectedMonth:yyyy-MM}";
                worksheet.Range("A1:L1").Merge();
                worksheet.Cell(1, 1).Style
                    .Font.SetBold()
                    .Font.SetFontSize(16)
                    .Font.SetFontColor(XLColor.Black)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(200, 220, 255));

                // Add a blank row
                worksheet.Row(2).Height = 10;

                // Set headers
                worksheet.Cell(3, 1).Value = "Mã NV";
                worksheet.Cell(3, 2).Value = "Họ Tên";
                worksheet.Cell(3, 3).Value = "Phòng Ban";
                worksheet.Cell(3, 4).Value = "Chức Vụ";
                worksheet.Cell(3, 5).Value = "Ngày Chấm Công";
                worksheet.Cell(3, 6).Value = "Ngày Nghỉ Phép";
                worksheet.Cell(3, 7).Value = "Ngày Thiếu";
                worksheet.Cell(3, 8).Value = "Tổng Ngày Làm";
                worksheet.Cell(3, 9).Value = "Ngày Tăng Ca";
                worksheet.Cell(3, 10).Value = "Tổng Giờ Chấm Công";
                worksheet.Cell(3, 11).Value = "Tổng Giờ Thiếu";
                worksheet.Cell(3, 12).Value = "Tổng Giờ Tăng Ca";

                // Style headers
                var headerRange = worksheet.Range("A3:L3");
                headerRange.Style
                    .Font.SetBold()
                    .Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromArgb(0, 102, 204))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetLeftBorderColor(XLColor.Black);

                // Populate data
                int row = 4;
                foreach (var emp in employees)
                {
                    var attendanceRecords = await _context.ChamCongs
                        .Where(c => c.MaNv == emp.MaNv &&
                                    c.NgayLamViec >= startDate &&
                                    c.NgayLamViec <= endDate &&
                                    (c.TrangThai == "CC3" || c.TrangThai == "CC4" || c.TrangThai == "CC5" || c.TrangThai == "CC6"))
                        .ToListAsync();

                    var leaveDays = await _context.NgayNghis
                        .Where(n => n.MaNv == emp.MaNv &&
                                    n.NgayNghi1 >= startDate &&
                                    n.NgayNghi1 <= endDate)
                        .ToListAsync();

                    var overtimeRecords = await _context.TangCas
                        .Where(t => t.MaNv == emp.MaNv &&
                                    t.NgayTangCa >= startDate &&
                                    t.NgayTangCa <= endDate &&
                                    t.TrangThai == "TC3")
                        .ToListAsync();

                    var missingHoursRecords = await _context.GioThieus
                        .Where(g => g.MaNv == emp.MaNv &&
                                    g.NgayThieu >= startDate &&
                                    g.NgayThieu <= endDate)
                        .ToListAsync();

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

                    var attendanceDays = attendanceRecords.Count;
                    var leaveDaysCount = leaveDays.Count;
                    var missingDays = workingDays - attendanceDays - leaveDaysCount;
                    var totalAttendanceHours = attendanceRecords.Sum(r => r.TongGio ?? 0m);
                    var totalMissingHours = missingHoursRecords.Sum(r => r.TongGioThieu);
                    var totalOvertimeHours = overtimeRecords.Sum(r => r.SoGioTangCa);

                    worksheet.Cell(row, 1).Value = emp.MaNv;
                    worksheet.Cell(row, 2).Value = emp.HoTen;
                    worksheet.Cell(row, 3).Value = emp.TenPhongBan;
                    worksheet.Cell(row, 4).Value = emp.TenChucVu;
                    worksheet.Cell(row, 5).Value = attendanceDays;
                    worksheet.Cell(row, 6).Value = leaveDaysCount;
                    worksheet.Cell(row, 7).Value = missingDays;
                    worksheet.Cell(row, 8).Value = workingDays;
                    worksheet.Cell(row, 9).Value = overtimeRecords.Count;
                    worksheet.Cell(row, 10).Value = totalAttendanceHours;
                    worksheet.Cell(row, 11).Value = totalMissingHours;
                    worksheet.Cell(row, 12).Value = totalOvertimeHours;

                    // Style data row
                    var dataRange = worksheet.Range($"A{row}:L{row}");
                    dataRange.Style
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                        .Border.SetLeftBorderColor(XLColor.Black)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                        .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                    // Format numeric columns to 2 decimal places
                    worksheet.Cell(row, 10).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(row, 11).Style.NumberFormat.Format = "0.00";
                    worksheet.Cell(row, 12).Style.NumberFormat.Format = "0.00";

                    row++;
                }

                // Auto-size columns with a minimum and maximum width
                foreach (var column in worksheet.Columns(1, 12))
                {
                    column.AdjustToContents();
                    if (column.Width < 10)
                        column.Width = 10;
                    if (column.Width > 30)
                        column.Width = 30;
                }

                // Adjust row heights for better readability
                worksheet.Rows(3, row - 1).Height = 20;

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                // Generate file name
                string fileName = $"BaoCaoChamCong_{selectedMonth:yyyy-MM}.xlsx";

                // Return file
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống khi xuất Excel.",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}