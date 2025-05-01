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

                // Lấy bản ghi chấm công (bỏ điều kiện trạng thái "Đã duyệt lần 1")
                var attendanceRecords = await _context.ChamCongs
                    .Where(c => c.MaNv == employeeId &&
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
                    _logger.LogInformation("Không có dữ liệu chấm công nào trong tháng này.");
                    return BadRequest(new { message = "Không có dữ liệu chấm công trong tháng này." });
                }

                // Lấy thông tin lương
                var salaryInfo = await _context.ThongTinLuongNVs
                    .Where(t => t.MaNv == employeeId && t.NgayApDung <= selectedMonth)
                    .OrderByDescending(t => t.NgayApDung)
                    .FirstOrDefaultAsync();

                if (salaryInfo == null)
                {
                    _logger.LogWarning($"Không tìm thấy thông tin lương cho nhân viên #{employeeId}");
                    return BadRequest(new { message = "Không tìm thấy thông tin lương." });
                }

                // Tính bảng lương
                var calculator = new PayrollCalculator(_context);
                var payroll = await calculator.CalculatePayroll(employeeId, selectedMonth, attendanceRecords, salaryInfo);

                // Kiểm tra bảng lương đã tồn tại chưa
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
                    existingPayroll.TrangThai = "Đã tạo - Chưa thanh toán";
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
                    .Include(e => e.MaChucVuNavigation)
                    .Include(e => e.MaPhongBanNavigation)
                    .Where(e => e.MaNv == employeeId)
                    .Select(e => new
                    {
                        e.HoTen,
                        e.Email,
                        TenChucVu = e.MaChucVuNavigation.TenChucVu,
                        TenPhongBan = e.MaPhongBanNavigation.TenPhongBan
                    })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    return BadRequest(new { message = "Không tìm thấy thông tin nhân viên." });
                }

                // Lấy thông tin lương
                var salaryInfo = await _context.ThongTinLuongNVs
                    .Where(t => t.MaNv == employeeId && t.NgayApDung <= selectedMonth)
                    .OrderByDescending(t => t.NgayApDung)
                    .FirstOrDefaultAsync();

                if (salaryInfo == null)
                {
                    return BadRequest(new { message = "Không tìm thấy thông tin lương." });
                }

                // Lấy trạng thái chấm công (bỏ điều kiện trạng thái "Đã duyệt lần 1")
                var attendanceRecords = await _context.ChamCongs
                    .Where(c => c.MaNv == employeeId &&
                                c.NgayLamViec.Year == selectedMonth.Year &&
                                c.NgayLamViec.Month == selectedMonth.Month)
                    .ToListAsync();

                int actualWorkingDays = 27; // Sửa cứng tạm thời: 27 ngày công thực tế
                decimal totalHours = 27 * 8; // Sửa cứng tạm thời: 27 ngày công * 8 giờ/ngày = 216 giờ

                // Lấy thông tin hợp đồng và tỷ lệ lương
                var contract = await _context.HopDongLaoDongs
                    .Include(hd => hd.LoaiHopDong)
                    .FirstOrDefaultAsync(hd => hd.MaNv == employeeId && hd.IsActive);

                decimal tiLeLuong = contract != null ? (decimal)(contract.LoaiHopDong.TiLeLuong ?? 1.0) : 1.0m;

                // Tính baseSalary thực tế (áp dụng TiLeLuong)
                decimal adjustedSalary = salaryInfo.LuongCoBan * tiLeLuong;
                decimal hourlyRate = adjustedSalary / 160;
                decimal actualBaseSalary = totalHours * hourlyRate;

                // Lấy thông tin tăng ca (bỏ điều kiện trạng thái "Đã duyệt lần 1")
                var overtimeRecords = await _context.TangCas
                    .Where(t => t.MaNv == employeeId &&
                                t.NgayTangCa.Year == selectedMonth.Year &&
                                t.NgayTangCa.Month == selectedMonth.Month)
                    .ToListAsync();
                decimal overtimeHours = overtimeRecords.Sum(t => (decimal)t.SoGioTangCa);
                decimal overtimePay = overtimeRecords.Sum(t => (decimal)t.SoGioTangCa * hourlyRate * t.TyLeTangCa);

                // Giả lập các khoản khấu trừ đi muộn, tạm ứng
                decimal lateDeduction = 0;
                decimal advanceDeduction = 0;

                // Tính ngày nghỉ phép (giả lập)
                int leaveDays = 0;
                int annualLeaveDays = 0;

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
                        standardWorkingDays = 20,
                        actualWorkingDays,
                        leaveDays,
                        annualLeaveDays,
                        baseSalary = salaryInfo.LuongCoBan,
                        actualBaseSalary,
                        bhxh = salaryInfo.BHXH,
                        bhyt = salaryInfo.BHYT,
                        bhtn = salaryInfo.BHTN,
                        fixedAllowance = salaryInfo.PhuCapCoDinh ?? 0,
                        additionalAllowance = payroll.PhuCapThem,
                        kpiBonus = payroll.LuongThem,
                        overtimeHours,
                        overtimePay,
                        totalIncome = payroll.TongLuong,
                        tax = payroll.ThueTNCN,
                        lateDeduction,
                        advanceDeduction,
                        netIncome = payroll.ThucNhan
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