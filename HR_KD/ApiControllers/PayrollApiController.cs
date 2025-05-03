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
//    [Route("api/[controller]")]
//    [ApiController]
//    public class PayrollApiController : ControllerBase
//    {
//        private readonly HrDbContext _context;
//        private readonly ILogger<PayrollApiController> _logger;

//        public PayrollApiController(HrDbContext context, ILogger<PayrollApiController> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        [HttpPost("GeneratePayroll")]
//        public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollRequest request)
//        {
//            try
//            {
//                int employeeId = request.EmployeeId;
//                string monthYear = request.MonthYear;

//                _logger.LogInformation($"Bắt đầu tạo bảng lương cho nhân viên ID: {employeeId}, tháng: {monthYear}");

//                if (string.IsNullOrEmpty(monthYear) || !DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime selectedMonth))
//                {
//                    return BadRequest(new { message = "Định dạng tháng không hợp lệ. Vui lòng sử dụng định dạng yyyy-MM." });
//                }

//                if (employeeId <= 0)
//                {
//                    return BadRequest(new { message = "Mã nhân viên không hợp lệ." });
//                }

//                var attendanceRecords = await _context.ChamCongs
//                    .Where(c => c.MaNv == employeeId &&
//                                c.NgayLamViec.Year == selectedMonth.Year &&
//                                c.NgayLamViec.Month == selectedMonth.Month)
//                    .ToListAsync();

//                _logger.LogInformation($"Số lượng bản ghi chấm công cho nhân viên #{employeeId} trong tháng {selectedMonth:yyyy-MM}: {attendanceRecords.Count}");
//                if (attendanceRecords.Any())
//                {
//                    _logger.LogInformation("Dữ liệu chấm công đầu tiên: {@AttendanceRecord}", attendanceRecords.First());
//                }

//                if (!attendanceRecords.Any())
//                {
//                    _logger.LogInformation("Không có dữ liệu chấm công nào trong tháng này, sẽ kiểm tra ngày lễ và ngày nghỉ.");
//                }

//                var salaryInfo = await _context.ThongTinLuongNVs
//                    .Where(t => t.MaNv == employeeId && t.NgayApDung <= selectedMonth)
//                    .OrderByDescending(t => t.NgayApDung)
//                    .FirstOrDefaultAsync();

//                if (salaryInfo == null)
//                {
//                    _logger.LogWarning($"Không tìm thấy thông tin lương cho nhân viên #{employeeId}");
//                    return BadRequest(new { message = "Không tìm thấy thông tin lương." });
//                }

//                var calculator = new PayrollCalculator(_context);
//                var payroll = await calculator.CalculatePayroll(employeeId, selectedMonth, attendanceRecords, salaryInfo);

//                var existingPayroll = await _context.BangLuongs
//                    .FirstOrDefaultAsync(b => b.MaNv == employeeId &&
//                                              b.ThangNam.Year == selectedMonth.Year &&
//                                              b.ThangNam.Month == selectedMonth.Month);

//                if (existingPayroll != null)
//                {
//                    existingPayroll.ThueTNCN = payroll.ThueTNCN;
//                    existingPayroll.LuongTangCa = payroll.LuongTangCa;
//                    existingPayroll.LuongThem = payroll.LuongThem;
//                    existingPayroll.PhuCapThem = payroll.PhuCapThem;
//                    existingPayroll.ThucNhan = payroll.ThucNhan;
//                    existingPayroll.TongLuong = payroll.TongLuong;
//                    existingPayroll.TrangThai = "Đã tạo - Chưa thanh toán";
//                    _context.BangLuongs.Update(existingPayroll);
//                }
//                else
//                {
//                    _context.BangLuongs.Add(payroll);
//                }

//                await _context.SaveChangesAsync();

//                _logger.LogInformation($"Hoàn thành tạo bảng lương cho nhân viên ID: {employeeId}, tháng: {monthYear}");

//                return Ok(new
//                {
//                    payroll.MaLuong,
//                    payroll.MaNv,
//                    payroll.ThangNam,
//                    payroll.TongLuong,
//                    payroll.ThueTNCN,
//                    payroll.ThucNhan,
//                    payroll.TrangThai
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi tạo bảng lương.");
//                return StatusCode(500, new { message = "Lỗi server.", error = ex.Message });
//            }
//        }

//        [HttpGet("GetPayrollDetails")]
//        public async Task<IActionResult> GetPayrollDetails(int employeeId, string monthYear)
//        {
//            try
//            {
//                if (!DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime selectedMonth))
//                {
//                    return BadRequest(new { message = "Định dạng tháng không hợp lệ." });
//                }

//                var payroll = await _context.BangLuongs
//                    .Where(b => b.MaNv == employeeId &&
//                                b.ThangNam.Year == selectedMonth.Year &&
//                                b.ThangNam.Month == selectedMonth.Month)
//                    .FirstOrDefaultAsync();

//                if (payroll == null)
//                {
//                    return Ok(new { hasPayroll = false });
//                }

//                var employee = await _context.NhanViens
//                    .Include(e => e.MaChucVuNavigation)
//                    .Include(e => e.MaPhongBanNavigation)
//                    .Where(e => e.MaNv == employeeId)
//                    .Select(e => new
//                    {
//                        e.HoTen,
//                        e.Email,
//                        TenChucVu = e.MaChucVuNavigation.TenChucVu,
//                        TenPhongBan = e.MaPhongBanNavigation.TenPhongBan,
//                        SoNguoiPhuThuoc = e.SoNguoiPhuThuoc
//                    })
//                    .FirstOrDefaultAsync();

//                if (employee == null)
//                {
//                    return BadRequest(new { message = "Không tìm thấy thông tin nhân viên." });
//                }

//                var salaryInfo = await _context.ThongTinLuongNVs
//                    .Where(t => t.MaNv == employeeId && t.NgayApDung <= selectedMonth)
//                    .OrderByDescending(t => t.NgayApDung)
//                    .FirstOrDefaultAsync();

//                if (salaryInfo == null)
//                {
//                    return BadRequest(new { message = "Không tìm thấy thông tin lương." });
//                }

//                var attendanceRecords = await _context.ChamCongs
//                    .Where(c => c.MaNv == employeeId &&
//                                c.NgayLamViec.Year == selectedMonth.Year &&
//                                c.NgayLamViec.Month == selectedMonth.Month)
//                    .ToListAsync();

//                decimal totalHours = attendanceRecords.Sum(a => a.TongGio ?? 0);
//                var attendanceDates = attendanceRecords.Select(a => a.NgayLamViec).ToHashSet();
//                int actualWorkingDays = attendanceRecords.Count;

//                var holidays = await _context.NgayLes
//                    .Where(h => h.NgayLe1.Year == selectedMonth.Year &&
//                                h.NgayLe1.Month == selectedMonth.Month &&
//                                (h.TrangThai == null || h.TrangThai != "Đã hủy"))
//                    .ToListAsync();

//                int holidayDays = 0;
//                foreach (var holiday in holidays)
//                {
//                    var holidayStart = holiday.NgayLe1;
//                    var holidayEnd = holiday.NgayLe1.AddDays((holiday.SoNgayNghi ?? 1) - 1);
//                    for (var date = holidayStart; date <= holidayEnd; date = date.AddDays(1))
//                    {
//                        if (date.Month == selectedMonth.Month && date.Year == selectedMonth.Year)
//                        {
//                            holidayDays++;
//                        }
//                    }
//                }

//                var approvedLeaves = await _context.NgayNghis
//                    .Include(n => n.MaLoaiNgayNghiNavigation)
//                    .Where(n => n.MaNv == employeeId &&
//                                n.NgayNghi1.Year == selectedMonth.Year &&
//                                n.NgayNghi1.Month == selectedMonth.Month &&
//                                n.MaTrangThai == 2 )
//                    .ToListAsync();

//                int daysInMonth = DateTime.DaysInMonth(selectedMonth.Year, selectedMonth.Month);
//                for (int day = 1; day <= daysInMonth; day++)
//                {
//                    var currentDate = new DateOnly(selectedMonth.Year, selectedMonth.Month, day);
//                    if (attendanceDates.Contains(currentDate))
//                        continue;

//                    bool isHoliday = holidays.Any(h =>
//                    {
//                        var holidayStart = h.NgayLe1;
//                        var holidayEnd = h.NgayLe1.AddDays((h.SoNgayNghi ?? 1) - 1);
//                        return currentDate >= holidayStart && currentDate <= holidayEnd;
//                    });

//                    bool isApprovedLeave = approvedLeaves.Any(l => l.NgayNghi1 == currentDate);

//                    if (isHoliday || isApprovedLeave)
//                    {
//                        totalHours += 8;
//                        actualWorkingDays++;
//                    }
//                }

//                var monthlyLeaves = approvedLeaves
//                    .Where(l => l.MaLoaiNgayNghiNavigation != null &&
//                                !l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Không lương") &&
//                                l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Tháng"))
//                    .Count();

//                var annualLeaves = approvedLeaves
//                    .Where(l => l.MaLoaiNgayNghiNavigation != null &&
//                                !l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Không lương") &&
//                                l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Năm"))
//                    .Count();

//                var otherLeaves = approvedLeaves
//                    .Where(l => l.MaLoaiNgayNghiNavigation != null &&
//                                !l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Không lương") &&
//                                !l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Tháng") &&
//                                !l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Năm"))
//                    .Count();

//                var unpaidLeaves = approvedLeaves
//                    .Where(l => l.MaLoaiNgayNghiNavigation != null &&
//                                l.MaLoaiNgayNghiNavigation.TenLoai.Contains("Không lương"))
//                    .Count();

//                var contract = await _context.HopDongLaoDongs
//                    .Include(hd => hd.LoaiHopDong)
//                    .FirstOrDefaultAsync(hd => hd.MaNv == employeeId && hd.IsActive);

//                decimal tiLeLuong = contract != null ? (decimal)(contract.LoaiHopDong.TiLeLuong ?? 1.0) : 1.0m;

//                int standardWorkingDays = 30;
//                decimal actualBaseSalary = salaryInfo.LuongCoBan * ((decimal)actualWorkingDays / standardWorkingDays) * tiLeLuong;

//                decimal hourlyRate = (salaryInfo.LuongCoBan * tiLeLuong) / 160;
//                var overtimeRecords = await _context.TangCas
//                    .Where(t => t.MaNv == employeeId &&
//                                t.NgayTangCa.Year == selectedMonth.Year &&
//                                t.NgayTangCa.Month == selectedMonth.Month)
//                    .ToListAsync();
//                decimal overtimeHours = overtimeRecords.Sum(t => (decimal)t.SoGioTangCa);
//                decimal overtimePay = overtimeRecords.Sum(t => (decimal)t.SoGioTangCa * hourlyRate * t.TyLeTangCa);

//                decimal lateDeduction = 0;
//                decimal advanceDeduction = 0;

//                return Ok(new
//                {
//                    hasPayroll = true,
//                    employee = new
//                    {
//                        employee.HoTen,
//                        employee.Email,
//                        employee.TenChucVu,
//                        employee.TenPhongBan,
//                        employee.SoNguoiPhuThuoc
//                    },
//                    payroll = new
//                    {
//                        monthYear,
//                        standardWorkingDays,
//                        actualWorkingDays,
//                        holidayDays,
//                        monthlyLeaveDays = monthlyLeaves,
//                        annualLeaveDays = annualLeaves,
//                        otherLeaveDays = otherLeaves,
//                        unpaidLeaveDays = unpaidLeaves,
//                        baseSalary = salaryInfo.LuongCoBan,
//                        actualBaseSalary,
//                        bhxh = salaryInfo.BHXH,
//                        bhyt = salaryInfo.BHYT,
//                        bhtn = salaryInfo.BHTN,
//                        fixedAllowance = salaryInfo.PhuCapCoDinh ?? 0,
//                        additionalAllowance = payroll.PhuCapThem,
//                        kpiBonus = payroll.LuongThem,
//                        overtimeHours,
//                        overtimePay,
//                        totalIncome = payroll.TongLuong,
//                        tax = payroll.ThueTNCN,
//                        lateDeduction,
//                        advanceDeduction,
//                        netIncome = payroll.ThucNhan
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi lấy thông tin bảng lương chi tiết.");
//                return StatusCode(500, new { message = "Lỗi server.", error = ex.Message });
//            }
//        }
//    }
//}