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
        public async Task<IActionResult> GetPayrollDetail(int maLuong)
        {
            var payroll = await _context.BangLuongs
                .Include(p => p.MaNvNavigation)
                    .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Include(p => p.MaNvNavigation.MaChucVuNavigation)
                .FirstOrDefaultAsync(p => p.MaLuong == maLuong);

            if (payroll == null)
                return NotFound("Không tìm thấy bảng lương.");

            var nv = payroll.MaNvNavigation;

            var thongTinLuong = await _context.ThongTinLuongNVs
                .Where(x => x.MaNv == nv.MaNv)
                .OrderByDescending(x => x.NgayApDung)
                .FirstOrDefaultAsync();

            var hopDong = await _context.HopDongLaoDongs
                .Include(h => h.LoaiHopDong)
                .Where(h => h.MaNv == nv.MaNv && h.IsActive)
                .OrderByDescending(h => h.NgayBatDau)
                .FirstOrDefaultAsync();

            var taiKhoan = await _context.TaiKhoanNganHangs
                .Where(t => t.MaNv == nv.MaNv)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                payroll.MaLuong,
                payroll.MaNv,
                HoTen = nv?.HoTen,
                GioiTinh = nv?.GioiTinh == true ? "Nam" : "Nữ",
                PhongBan = nv?.MaPhongBanNavigation?.TenPhongBan ?? "N/A",
                ChucVu = nv?.MaChucVuNavigation?.TenChucVu ?? "N/A",
                LoaiHopDong = hopDong?.LoaiHopDong?.TenLoaiHopDong ?? "Không có",
                payroll.ThangNam,
                // B. THU NHẬP
                ThuNhap = new
                {
                    LuongCoBan = thongTinLuong?.LuongCoBan ?? 0,
                    PhuCapCoDinh = thongTinLuong?.PhuCapCoDinh ?? 0,
                    ThuongCoDinh = thongTinLuong?.ThuongCoDinh ?? 0,
                    PhuCapThem = payroll.PhuCapThem,
                    LuongThem = payroll.LuongThem,
                    LuongTangCa = payroll.LuongTangCa,
                    TongLuong = payroll.TongLuong ?? 0
                },
                // C. KHẤU TRỪ
                KhauTru = new
                {
                    BHXH = thongTinLuong?.BHXH ?? 0,
                    BHYT = thongTinLuong?.BHYT ?? 0,
                    BHTN = thongTinLuong?.BHTN ?? 0,
                    ThueTNCN = payroll.ThueTNCN
                },
                // D. THỰC LÃNH
                payroll.ThucNhan,
                // E. THÔNG TIN TÀI KHOẢN
                TaiKhoanNganHang = taiKhoan != null ? new
                {
                    taiKhoan.TenNganHang,
                    taiKhoan.ChiNhanh,
                    taiKhoan.SoTaiKhoan
                } : null,
                // F. GHI CHÚ
                payroll.GhiChu,
                payroll.TrangThai,
                TenTrangThai = _statusNameMapping.TryGetValue(payroll.TrangThai, out var name) ? name : payroll.TrangThai,
                NgayIn = DateTime.Now
            });
        }
        #endregion
    }
}