using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly PayrollCalculator _calculator;
        private Dictionary<string, string> _statusNameMapping; // Dictionary để lưu trữ mã trạng thái - tên trạng thái

        public PayrollApiController(HrDbContext context, ILogger<PayrollApiController> logger, PayrollCalculator calculator)
        {
            _context = context;
            _logger = logger;
            _calculator = calculator;

            // Tải dữ liệu trạng thái khi controller được tạo
            _statusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
        }

        #region Xử lý tạo hoặc lấy bảng lương theo từng phòng ban (Admin có thể dùng)
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
            payroll.TrangThai = "BL1"; // Trạng thái ban đầu là "Đã tạo"

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

        #region xem chi tiết bảng lương (cho tất cả các cấp)
        [HttpGet("GetPayrollDetail/{maLuong}")]
        public async Task<IActionResult> GetPayrollDetail(int maLuong)
        {
            var payroll = await _context.BangLuongs
                .Include(x => x.MaNvNavigation)
                .FirstOrDefaultAsync(x => x.MaLuong == maLuong);

            if (payroll == null)
                return NotFound("Không tìm thấy bảng lương.");

            return Ok(new
            {
                payroll.MaLuong,
                payroll.MaNv,
                HoTen = payroll.MaNvNavigation?.HoTen,
                payroll.ThangNam,
                payroll.TongLuong,
                payroll.LuongThem,
                payroll.PhuCapThem,
                payroll.LuongTangCa,
                payroll.ThueTNCN,
                payroll.ThucNhan,
                payroll.TrangThai,
                TenTrangThai = _statusNameMapping.TryGetValue(payroll.TrangThai, out var name) ? name : payroll.TrangThai,
                payroll.GhiChu
            });
        }
        #endregion

        #region Bảng lương cá nhân (cho nhân viên)
        [HttpGet("MyPayroll")]
        public async Task<IActionResult> GetMyPayroll()
        {
            var username = User.Identity?.Name;
            var user = await _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .FirstOrDefaultAsync(t => t.Username == username);

            if (user == null) return Unauthorized();

            int employeeId = user.MaNv;

            var payrolls = await _context.BangLuongs
                .Where(b => b.MaNv == employeeId)
                .OrderByDescending(b => b.ThangNam)
                .ToListAsync();

            if (!payrolls.Any())
            {
                return Ok(new { hasPayroll = false, message = "Chưa có bảng lương nào." });
            }

            var result = payrolls
                .GroupBy(p => p.ThangNam.Year)
                .OrderByDescending(g => g.Key)
                .Select(yearGroup => new
                {
                    Year = yearGroup.Key,
                    Months = yearGroup.OrderByDescending(p => p.ThangNam.Month)
                                     .Select(p => new
                                     {
                                         p.MaLuong,
                                         MonthYear = p.ThangNam.ToString("yyyy-MM"),
                                         DisplayMonthYear = $"Tháng {p.ThangNam.Month} - {p.ThangNam.Year}",
                                         p.TongLuong,
                                         p.ThucNhan,
                                         p.TrangThai,
                                         TenTrangThai = _statusNameMapping.TryGetValue(p.TrangThai, out var name) ? name : p.TrangThai,
                                         p.LuongTangCa,
                                         p.LuongThem,
                                         p.PhuCapThem,
                                         p.ThueTNCN
                                     })
                });

            return Ok(new { hasPayroll = true, data = result });
        }

        [HttpPost("EmployeeConfirmPayroll/{maLuong}")]
        public async Task<IActionResult> EmployeeConfirmPayroll(int maLuong)
        {
            var payroll = await _context.BangLuongs.FindAsync(maLuong);
            if (payroll == null) return NotFound("Không tìm thấy bảng lương.");

            if (payroll.TrangThai != "BL1")
                return BadRequest("Không thể xác nhận bảng lương ở trạng thái hiện tại.");

            payroll.TrangThai = "BL1A";
            await _context.SaveChangesAsync();
            return Ok("Bảng lương đã được xác nhận.");
        }

        [HttpPost("EmployeeRejectPayroll/{maLuong}")]
        public async Task<IActionResult> EmployeeRejectPayroll(int maLuong, [FromBody] string? lyDo)
        {
            var payroll = await _context.BangLuongs.FindAsync(maLuong);
            if (payroll == null) return NotFound("Không tìm thấy bảng lương.");

            if (payroll.TrangThai != "BL1") // Chỉ được từ chối khi ở trạng thái "Đã tạo"
                return BadRequest("Không thể từ chối bảng lương ở trạng thái hiện tại.");

            payroll.TrangThai = "BL1R"; // Nhân viên từ chối
            payroll.GhiChu = lyDo;
            await _context.SaveChangesAsync();
            return Ok("Đã gửi yêu cầu điều chỉnh.");
        }
        #endregion

        #region Tạo báo cáo chi tiết (chưa triển khai logic)
        [HttpGet("CreatePayrollDetailReport/{maLuong}")]
        public async Task<IActionResult> CreatePayrollDetailReport(int maLuong)
        {
            // TODO: Implement logic to generate the detailed payroll report
            return Ok(new { message = $"Chức năng tạo báo cáo chi tiết cho bảng lương có mã {maLuong} đang được phát triển." });
        }
        #endregion
    }
}