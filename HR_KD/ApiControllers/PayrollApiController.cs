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
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly ILogger<PayrollApiController> _logger;

        public PayrollApiController(HrDbContext context, ILogger<PayrollApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("GetPayroll")]
        public async Task<IActionResult> GetPayroll(int employeeId, string monthYear)
        {
            try
            {
                if (!DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime selectedMonth))
                {
                    return BadRequest(new { message = "Định dạng tháng không hợp lệ." });
                }

                // Kiểm tra thông tin lương nhân viên
                var salaryInfo = await _context.ThongTinLuongNVs
                    .Where(t => t.MaNv == employeeId && t.NgayApDng <= DateTime.Now)
                    .OrderByDescending(t => t.NgayApDng)
                    .FirstOrDefaultAsync();

                if (salaryInfo == null)
                {
                    return Ok(new { status = "Thông tin lương nhân viên chưa được cập nhật" });
                }

                // Lấy bảng lương
                var payroll = await _context.BangLuongs
                    .Where(b => b.MaNv == employeeId &&
                                b.ThangNam.Year == selectedMonth.Year &&
                                b.ThangNam.Month == selectedMonth.Month)
                    .Select(b => new
                    {
                        b.TongLuong,
                        b.ThucNhan,
                        TongTienThue = b.TongLuong - b.ThucNhan
                    })
                    .FirstOrDefaultAsync();

                if (payroll == null)
                {
                    return Ok(new { status = "Thông tin lương nhân viên chưa được cập nhật" });
                }

                return Ok(new
                {
                    status = "Đã có bảng lương",
                    payroll.TongLuong,
                    payroll.ThucNhan,
                    payroll.TongTienThue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin bảng lương.");
                return StatusCode(500, new { message = "Lỗi server.", error = ex.Message });
            }
        }

        [HttpPost("GeneratePayroll")]
        public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRequest request)
        {
            try
            {
                int employeeId = request.EmployeeId;
                string monthYear = request.MonthYear;

                _logger.LogInformation($"Bắt đầu tạo bảng lương cho nhân viên ID: {employeeId}, tháng: {monthYear}");

                if (string.IsNullOrEmpty(monthYear) || !DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime selectedMonth))
                {
                    return BadRequest(new { message = "Định dạng tháng không hợp lệ. Vui lòng sử dụng định dạng yyyy-MM." });
                }

                if (employeeId <= 0)
                {
                    return BadRequest(new { message = "Mã nhân viên không hợp lệ." });
                }

                var attendanceRecords = await _context.ChamCongs
                    .Where(c => c.MaNv == employeeId &&
                                c.TrangThai.Trim() == "Đã duyệt" &&
                                c.NgayLamViec.Year == selectedMonth.Year &&
                                c.NgayLamViec.Month == selectedMonth.Month)
                    .ToListAsync();

                _logger.LogInformation($"Số lượng bản ghi chấm công cho nhân viên #{employeeId} trong tháng {selectedMonth:yyyy-MM}: {attendanceRecords.Count}");
                if (attendanceRecords.Any())
                {
                    _logger.LogInformation("Dữ liệu chấm công đầu tiên: {@AttendanceRecord}", attendanceRecords.First());
                }

                if (!attendanceRecords.Any())
                {
                    var allRecords = await _context.ChamCongs
                        .Where(c => c.MaNv == employeeId &&
                                    c.NgayLamViec.Year == selectedMonth.Year &&
                                    c.NgayLamViec.Month == selectedMonth.Month)
                        .ToListAsync();

                    if (allRecords.Any())
                    {
                        _logger.LogInformation($"Có {allRecords.Count} bản ghi chấm công, nhưng không có bản ghi nào có trạng thái 'Đã duyệt'. Trạng thái mẫu: {allRecords.First().TrangThai}");
                        return BadRequest(new { message = "Có dữ liệu chấm công nhưng không có bản ghi nào được duyệt." });
                    }
                    else
                    {
                        _logger.LogInformation("Không có dữ liệu chấm công nào trong tháng này.");
                        return BadRequest(new { message = "Không có dữ liệu chấm công trong tháng này." });
                    }
                }

                var salaryInfo = await _context.ThongTinLuongNVs
                    .Where(t => t.MaNv == employeeId && t.NgayApDng <= DateTime.Now)
                    .OrderByDescending(t => t.NgayApDng)
                    .FirstOrDefaultAsync();

                if (salaryInfo == null)
                {
                    _logger.LogWarning($"Không tìm thấy thông tin lương cho nhân viên #{employeeId}");
                    return BadRequest(new { message = "Không tìm thấy thông tin lương." });
                }

                var calculator = new PayrollCalculator(_context);
                var payroll = calculator.CalculatePayroll(employeeId, selectedMonth, attendanceRecords, salaryInfo);

                var existingPayroll = await _context.BangLuongs
                    .FirstOrDefaultAsync(b => b.MaNv == employeeId &&
                                              b.ThangNam.Year == selectedMonth.Year &&
                                              b.ThangNam.Month == selectedMonth.Month);

                if (existingPayroll != null)
                {
                    existingPayroll.ThueTNCN = payroll.ThueTNCN;
                    existingPayroll.LuongTangCa = payroll.LuongTangCa;
                    existingPayroll.LuongThem = payroll.LuongThem;
                    existingPayroll.PhuCapThem = payroll.PhuCapThem;
                    existingPayroll.ThucNhan = payroll.ThucNhan;
                    existingPayroll.TongLuong = payroll.TongLuong;
                    existingPayroll.TrangThai = "Đã tạo";
                    _context.BangLuongs.Update(existingPayroll);
                }
                else
                {
                    _context.BangLuongs.Add(payroll);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Hoàn thành tạo bảng lương cho nhân viên ID: {employeeId}, tháng: {monthYear}");

                return Ok(new
                {
                    payroll.MaLuong,
                    payroll.MaNv,
                    payroll.ThangNam,
                    payroll.TongLuong,
                    payroll.ThueTNCN,
                    payroll.ThucNhan,
                    payroll.TrangThai
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo bảng lương.");
                return StatusCode(500, new { message = "Lỗi server.", error = ex.Message });
            }
        }

        [HttpGet("GetPayrollDetails")]
        public async Task<IActionResult> GetPayrollDetails(int employeeId, string monthYear)
        {
            try
            {
                if (!DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime selectedMonth))
                {
                    return BadRequest(new { message = "Định dạng tháng không hợp lệ." });
                }

                // Kiểm tra xem nhân viên đã có bảng lương chưa
                var payroll = await _context.BangLuongs
                    .Where(b => b.MaNv == employeeId &&
                                b.ThangNam.Year == selectedMonth.Year &&
                                b.ThangNam.Month == selectedMonth.Month)
                    .FirstOrDefaultAsync();

                if (payroll == null)
                {
                    return Ok(new { hasPayroll = false });
                }

                // Lấy thông tin nhân viên
                var employee = await _context.NhanViens
                    .Where(e => e.MaNv == employeeId)
                    .Select(e => new
                    {
                        e.HoTen,
                        e.Email,
                        // With this corrected line:  
                        e.MaChucVuNavigation.TenChucVu,
                        e.MaPhongBanNavigation.TenPhongBan
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return BadRequest(new { message = "Không tìm thấy thông tin nhân viên." });
                }

                // Lấy thông tin lương từ ThongTinLuongNVs
                var salaryInfo = await _context.ThongTinLuongNVs
                    .Where(t => t.MaNv == employeeId && t.NgayApDng <= DateTime.Now)
                    .OrderByDescending(t => t.NgayApDng)
                    .FirstOrDefaultAsync();

                if (salaryInfo == null)
                {
                    return BadRequest(new { message = "Không tìm thấy thông tin lương." });
                }

                // Lấy trạng thái chấm công (để tính ngày công thực tế)
                var attendanceRecords = await _context.ChamCongs
                    .Where(c => c.MaNv == employeeId &&
                                c.TrangThai.Trim() == "Đã duyệt" &&
                                c.NgayLamViec.Year == selectedMonth.Year &&
                                c.NgayLamViec.Month == selectedMonth.Month)
                    .ToListAsync();

                int actualWorkingDays = attendanceRecords.Count;

                // Tính baseSalary (theo logic trong PayrollCalculator)
                decimal totalHours = attendanceRecords.Sum(a => a.TongGio ?? 0);
                decimal hourlyRate = salaryInfo.LuongCoBan / 160;
                decimal baseSalary = totalHours * hourlyRate;

                return Ok(new
                {
                    hasPayroll = true,
                    employee = new
                    {
                        employee.HoTen,
                        employee.Email,
                        employee.TenChucVu,
                        employee.TenPhongBan
                    },
                    payroll = new
                    {
                        monthYear,
                        standardWorkingDays = 20, // Hardcode 20 ngày công chuẩn
                        actualWorkingDays,
                        leaveDays = 0, // Nghỉ phép tháng
                        annualLeaveDays = 0, // Phép năm
                        baseSalary = salaryInfo.LuongCoBan, // Lương cơ bản
                        actualBaseSalary = baseSalary, // Lương thực tế (baseSalary)
                        bhxh = salaryInfo.BHXH,
                        bhyt = salaryInfo.BHYT,
                        bhtn = salaryInfo.BHTN,
                        fixedAllowance = salaryInfo.PhuCapCoDinh ?? 0, // Phụ cấp cố định
                        additionalAllowance = payroll.PhuCapThem, // Phụ cấp thêm
                        kpiBonus = payroll.LuongThem, // Thưởng KPI
                        overtimeHours = 0, // Giờ làm thêm
                        overtimePay = 0, // Tiền làm thêm
                        totalIncome = payroll.TongLuong, // Tổng thu nhập
                        tax = payroll.ThueTNCN, // Thuế TNCN
                        lateDeduction = 0, // Đi muộn
                        advanceDeduction = 0, // Tạm ứng
                        netIncome = payroll.ThucNhan // Thực nhận
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin bảng lương chi tiết.");
                return StatusCode(500, new { message = "Lỗi server.", error = ex.Message });
            }
        }
    }
}