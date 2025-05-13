using HR_KD.Data;
using HR_KD.DTOs;
using HR_KD.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class PayrollApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly ILogger<PayrollApiController> _logger;
        private readonly PayrollCalculator _calculator;
        private Dictionary<string, string> _statusNameMapping;

        public PayrollApiController(HrDbContext context, ILogger<PayrollApiController> logger, PayrollCalculator calculator)
        {
            _context = context;
            _logger = logger;
            _calculator = calculator;
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
                        CoBangLuong = bangLuongs.Any(bl => bl.MaNv == nv.MaNv),
                        MaLuong = bangLuongs.FirstOrDefault(bl => bl.MaNv == nv.MaNv)?.MaLuong,
                        TrangThai = bangLuongs.FirstOrDefault(bl => bl.MaNv == nv.MaNv)?.TrangThai,
                        TenTrangThai = _statusNameMapping.TryGetValue(bangLuongs.FirstOrDefault(bl => bl.MaNv == nv.MaNv)?.TrangThai ?? "", out var name) ? name : (bangLuongs.FirstOrDefault(bl => bl.MaNv == nv.MaNv)?.TrangThai ?? "")
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
                // No need for PayrollDto here, just return info
                return Ok(new { exists = true, maLuong = existing.MaLuong });
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


            var payroll = await _calculator.CalculatePayroll(maNv, monthDate, chamCongs, thongTinLuong);

            payroll.NguoiTao = User.Identity?.Name ?? "System";
            payroll.NgayTao = DateTime.Now;
            payroll.TrangThai = PayrollStatus.Created; // Initial status

            _context.BangLuongs.Add(payroll);
            await _context.SaveChangesAsync();

            return Ok(new { created = true, maLuong = payroll.MaLuong });
        }

        [HttpPost("CreateBulkPayroll")]
        public async Task<IActionResult> CreateBulkPayroll(int year, int month)
        {
            var monthDate = new DateTime(year, month, 1);
            var nhanViensChuaCoBangLuong = await _context.NhanViens
                .Where(nv => !_context.BangLuongs.Any(bl => bl.MaNv == nv.MaNv && bl.ThangNam.Year == year && bl.ThangNam.Month == month))
                .ToListAsync();

            int count = 0;
            foreach (var nv in nhanViensChuaCoBangLuong)
            {
                var chamCongs = await _context.ChamCongs
                    .Where(c => c.MaNv == nv.MaNv &&
                                 c.NgayLamViec.Year == year &&
                                 c.NgayLamViec.Month == month)
                    .ToListAsync();

                if (!chamCongs.Any())
                {
                    _logger.LogWarning($"No timekeeping data for employee {nv.MaNv} in month {month}/{year}. Skipping.");
                    continue;
                }

                var thongTinLuong = await _context.ThongTinLuongNVs
                    .Where(x => x.MaNv == nv.MaNv)
                    .OrderByDescending(x => x.NgayApDung)
                    .FirstOrDefaultAsync();

                if (thongTinLuong == null)
                {
                    _logger.LogWarning($"No salary information found for employee {nv.MaNv}. Skipping bulk creation for this employee.");
                    continue; // Skip this employee if no salary info
                }

                var payroll = await _calculator.CalculatePayroll(nv.MaNv, monthDate, chamCongs, thongTinLuong);
                payroll.NguoiTao = User.Identity?.Name ?? "System";
                payroll.NgayTao = DateTime.Now;
                payroll.TrangThai = PayrollStatus.Created; // Initial status

                _context.BangLuongs.Add(payroll);
                count++;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Bulk payroll creation completed. Created {count} new payrolls for month {month}/{year}.");

            return Ok($"Đã tạo {count} bảng lương mới.");
        }
        #endregion

        #region xem chi tiết bảng lương (cho tất cả các cấp)
        [HttpGet("GetPayrollDetail/{maLuong}")]
        // Keep it open or restrict based on requirement, e.g., [Authorize]
        public async Task<IActionResult> GetPayrollDetail(int maLuong)
        {
            var payroll = await _context.BangLuongs
                .Include(x => x.MaNvNavigation)
                .FirstOrDefaultAsync(x => x.MaLuong == maLuong);

            if (payroll == null)
                return NotFound("Không tìm thấy bảng lương.");

            // You might want to add authorization here to ensure the user has permission to view this specific payroll (e.g., is the employee, their manager, accountant, or director)

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
    }
}