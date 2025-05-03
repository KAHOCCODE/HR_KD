using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly ILogger<PayrollApiController> _logger;
        private readonly PayrollCalculator _calculator;

        public PayrollApiController(HrDbContext context, ILogger<PayrollApiController> logger, PayrollCalculator calculator)
        {
            _context = context;
            _logger = logger;
            _calculator = calculator;
        }

        #region Xử lý tạo hoặc lấy bảng lương theo từng phòng ban
        [HttpGet("GetPayrollStatus")]
        public async Task<IActionResult> GetPayrollStatus(int year, int month)
        {
            var nhanViens = await _context.NhanViens
                .Include(nv => nv.MaPhongBanNavigation)
                .ToListAsync();

            var bangLuongs = await _context.BangLuongs
                .Where(x => x.ThangNam.Year == year && x.ThangNam.Month == month)
                .ToListAsync();

            var data = nhanViens
                .GroupBy(nv => nv.MaPhongBanNavigation.TenPhongBan)
                .Select(group => new
                {
                    TenPhongBan = group.Key,
                    NhanViens = group.Select(nv => new
                    {
                        nv.MaNv,
                        nv.HoTen,
                        CoBangLuong = bangLuongs.Any(bl => bl.MaNv == nv.MaNv)
                    })
                });

            return Ok(data);
        }

        [HttpPost("CreateOrGetPayroll")]
        public async Task<IActionResult> CreateOrGetPayroll(int maNv, int year, int month)
        {
            var monthDate = new DateTime(year, month, 1);

            var existing = await _context.BangLuongs
                .FirstOrDefaultAsync(x => x.MaNv == maNv && x.ThangNam.Year == year && x.ThangNam.Month == month);

            if (existing != null)
            {
                var existingDto = new PayrollDto
                {
                    MaLuong = existing.MaLuong,
                    MaNv = existing.MaNv,
                    ThangNam = existing.ThangNam,
                    PhuCapThem = existing.PhuCapThem,
                    LuongThem = existing.LuongThem,
                    LuongTangCa = existing.LuongTangCa,
                    ThueTNCN = existing.ThueTNCN,
                    TongLuong = existing.TongLuong,
                    ThucNhan = existing.ThucNhan,
                    NguoiTao = existing.NguoiTao,
                    NgayTao = existing.NgayTao,
                    TrangThai = existing.TrangThai,
                    GhiChu = existing.GhiChu
                };
                return Ok(new { exists = true, data = existingDto });
            }

            var chamCongs = await _context.ChamCongs
                .Where(c => c.MaNv == maNv &&
                            c.NgayLamViec.Year == year &&
                            c.NgayLamViec.Month == month)
                .ToListAsync();

            if (!chamCongs.Any())
                return BadRequest("Không có dữ liệu chấm công trong tháng.");

            var thongTinLuong = await _context.ThongTinLuongNVs
                .Where(x => x.MaNv == maNv)
                .OrderByDescending(x => x.NgayApDung)
                .FirstOrDefaultAsync();

            if (thongTinLuong == null)
                return BadRequest("Không tìm thấy thông tin lương cho nhân viên.");

            var giamtrugiacanh = await _context.GiamTruGiaCanhs
                .Where(x => x.NgayHieuLuc <= monthDate && (x.NgayHetHieuLuc == null || x.NgayHetHieuLuc >= monthDate))
                .OrderByDescending(x => x.NgayHieuLuc)
                .FirstOrDefaultAsync();
            if (giamtrugiacanh == null)
                return BadRequest("Không tìm thấy thông tin giảm trừ gia cảnh.");

            var payroll = await _calculator.CalculatePayroll(maNv, monthDate, chamCongs, thongTinLuong);

            payroll.NguoiTao = User.Identity?.Name ?? "System";
            payroll.NgayTao = DateTime.Now;

            _context.BangLuongs.Add(payroll);
            await _context.SaveChangesAsync();

            var payrollDto = new PayrollDto
            {
                MaLuong = payroll.MaLuong,
                MaNv = payroll.MaNv,
                ThangNam = payroll.ThangNam,
                PhuCapThem = payroll.PhuCapThem,
                LuongThem = payroll.LuongThem,
                LuongTangCa = payroll.LuongTangCa,
                ThueTNCN = payroll.ThueTNCN,
                TongLuong = payroll.TongLuong,
                ThucNhan = payroll.ThucNhan,
                NguoiTao = payroll.NguoiTao,
                NgayTao = payroll.NgayTao,
                TrangThai = payroll.TrangThai,
                GhiChu = payroll.GhiChu
            };
            return Ok(new { created = true, data = payrollDto });
        }
        #endregion

        #region xem chi tiết bảng lương
        [HttpGet("GetPayrollDetail")]
        public async Task<IActionResult> GetPayrollDetail(int maNv, int year, int month)
        {
            var payroll = await _context.BangLuongs
                .Include(x => x.MaNvNavigation)
                .FirstOrDefaultAsync(x => x.MaNv == maNv && x.ThangNam.Year == year && x.ThangNam.Month == month);

            if (payroll == null)
                return NotFound("Không tìm thấy bảng lương.");

            return Ok(new
            {
                payroll.MaNv,
                HoTen = payroll.MaNvNavigation?.HoTen,
                payroll.ThangNam,
                payroll.TongLuong,
                payroll.LuongThem,
                payroll.PhuCapThem,
                payroll.LuongTangCa,
                payroll.ThueTNCN,
                payroll.ThucNhan,
                payroll.GhiChu
            });
        }
        #endregion

        #region GetMyPayroll
        [HttpGet("MyPayroll")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyPayroll(string monthYear)
        {
            var username = User.Identity?.Name;
            var user = await _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .FirstOrDefaultAsync(t => t.Username == username);

            if (user == null) return Unauthorized();

            int employeeId = user.MaNv;
            if (!DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out DateTime selectedMonth))
            {
                return BadRequest(new { message = "Tháng không hợp lệ" });
            }

            var payroll = await _context.BangLuongs
                .FirstOrDefaultAsync(b => b.MaNv == employeeId &&
                                            b.ThangNam.Year == selectedMonth.Year &&
                                            b.ThangNam.Month == selectedMonth.Month);

            if (payroll == null)
            {
                return Ok(new { hasPayroll = false, message = "Chưa có bảng lương trong tháng này." });
            }

            return Ok(new
            {
                hasPayroll = true,
                thang = monthYear,
                tongLuong = payroll.TongLuong,
                thucNhan = payroll.ThucNhan,
                luongTangCa = payroll.LuongTangCa,
                luongThem = payroll.LuongThem,
                phuCapThem = payroll.PhuCapThem,
                thueTNCN = payroll.ThueTNCN,
                trangThai = payroll.TrangThai
            });
        }
        #endregion
    }
}