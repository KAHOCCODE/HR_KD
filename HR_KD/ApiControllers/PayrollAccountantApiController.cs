using HR_KD.Common;
using HR_KD.Data;
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
    public class PayrollAccountantApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly EmailService _emailService;
        private readonly PayrollCalculator _payrollCalculator;
        private readonly Dictionary<string, string> _statusNameMapping;

        public PayrollAccountantApiController(HrDbContext context, EmailService emailService, PayrollCalculator payrollCalculator)
        {
            _context = context;
            _emailService = emailService;
            _payrollCalculator = payrollCalculator;
            _statusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
        }

        // Lấy danh sách bảng lương cần duyệt (BL2)
        [HttpGet("GetPayrolls")]
        public async Task<IActionResult> GetPayrolls(int year, int month, int? departmentId)
        {
            var query = _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.TrangThai == "BL2");

            if (departmentId.HasValue)
                query = query.Where(b => b.MaNvNavigation.MaPhongBan == departmentId.Value);

            var bangLuongs = await query.ToListAsync();

            var result = bangLuongs.GroupBy(b => b.MaNvNavigation.MaPhongBanNavigation.TenPhongBan)
                .Select(g => new
                {
                    MaPhongBan = g.First().MaNvNavigation.MaPhongBan,
                    TenPhongBan = g.Key,
                    Payrolls = g.Select(b => new
                    {
                        b.MaLuong,
                        b.MaNv,
                        HoTen = b.MaNvNavigation.HoTen,
                        b.ThucNhan,
                        b.TrangThai,
                        TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
                    }).OrderBy(p => p.MaNv)
                })
                .OrderBy(g => g.TenPhongBan);

            return Ok(result);
        }

        // Lấy danh sách yêu cầu chỉnh sửa từ nhân viên (BL1R)
        [HttpGet("GetRevisionRequests")]
        public async Task<IActionResult> GetRevisionRequests(int year, int month, int? departmentId)
        {
            var query = _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.TrangThai == "BL1R");

            if (departmentId.HasValue)
                query = query.Where(b => b.MaNvNavigation.MaPhongBan == departmentId.Value);

            var bangLuongs = await query.ToListAsync();

            var result = bangLuongs.Select(b => new
            {
                b.MaLuong,
                b.MaNv,
                HoTen = b.MaNvNavigation.HoTen,
                b.ThucNhan,
                b.GhiChu,
                b.TrangThai,
                TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
            }).OrderBy(b => b.MaNv);

            return Ok(result);
        }

        // Lấy danh sách bảng lương trả về từ giám đốc (BL3R)
        [HttpGet("GetDirectorRejections")]
        public async Task<IActionResult> GetDirectorRejections(int year, int month, int? departmentId)
        {
            var query = _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.TrangThai == "BL3R");

            if (departmentId.HasValue)
                query = query.Where(b => b.MaNvNavigation.MaPhongBan == departmentId.Value);

            var bangLuongs = await query.ToListAsync();

            var result = bangLuongs.Select(b => new
            {
                b.MaLuong,
                b.MaNv,
                HoTen = b.MaNvNavigation.HoTen,
                b.ThucNhan,
                b.GhiChu,
                b.TrangThai,
                TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
            }).OrderBy(b => b.MaNv);

            return Ok(result);
        }

        // Xử lý yêu cầu chỉnh sửa từ nhân viên
        [HttpPost("HandleEmployeeRevisionRequest/{maLuong}")]
        public async Task<IActionResult> HandleEmployeeRevisionRequest(int maLuong, [FromBody] RevisionRequestAction action)
        {
            var payroll = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .FirstOrDefaultAsync(b => b.MaLuong == maLuong && b.TrangThai == "BL1R");

            if (payroll == null)
                return BadRequest("Bảng lương không tồn tại hoặc không ở trạng thái yêu cầu chỉnh sửa (BL1R).");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            if (action.Action == "APPROVE")
            {
                // Cập nhật chấm công bổ sung
                var attendanceRecords = await _context.ChamCongs
                    .Where(c => c.MaNv == payroll.MaNv && c.NgayLamViec.Year == payroll.ThangNam.Year && c.NgayLamViec.Month == payroll.ThangNam.Month)
                    .ToListAsync();

                // Giả sử thông tin chấm công bổ sung được cung cấp trong action.Data (JSON chứa danh sách chấm công mới)
                if (action.Data != null)
                {
                    var newAttendances = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ChamCong>>(action.Data);
                    foreach (var attendance in newAttendances)
                    {
                        attendance.TrangThai = AttendanceStatus.Approved;
                        attendance.MaNvDuyet = int.Parse(currentUser); // Giả sử currentUser là MaNv
                        attendance.NgayDuyet = currentTime;
                        _context.ChamCongs.Add(attendance);
                    }
                }

                // Tính lại bảng lương
                var salaryInfo = await _context.ThongTinLuongNVs
                    .FirstOrDefaultAsync(t => t.MaNv == payroll.MaNv && t.IsActive);
                if (salaryInfo == null)
                    return BadRequest("Không tìm thấy thông tin lương của nhân viên.");

                var updatedAttendances = await _context.ChamCongs
                    .Where(c => c.MaNv == payroll.MaNv && c.NgayLamViec.Year == payroll.ThangNam.Year && c.NgayLamViec.Month == payroll.ThangNam.Month)
                    .ToListAsync();

                var updatedPayroll = await _payrollCalculator.CalculatePayroll(
                    payroll.MaNv,
                    new DateTime(payroll.ThangNam.Year, payroll.ThangNam.Month, 1),
                    updatedAttendances,
                    salaryInfo
                );

                payroll.TongLuong = updatedPayroll.TongLuong;
                payroll.LuongTangCa = updatedPayroll.LuongTangCa;
                payroll.PhuCapThem = updatedPayroll.PhuCapThem;
                payroll.LuongThem = updatedPayroll.LuongThem;
                payroll.ThueTNCN = updatedPayroll.ThueTNCN;
                payroll.ThucNhan = updatedPayroll.ThucNhan;
                payroll.TrangThai = "BL2"; // Chuyển lại trạng thái chờ kế toán duyệt
                payroll.GhiChu = action.Reason;
                payroll.NguoiDuyetKT = null;
                payroll.NgayDuyetKT = null;
            }
            else if (action.Action == "REJECT")
            {
                // Gửi email thông báo từ chối
                var employee = payroll.MaNvNavigation;
                if (string.IsNullOrWhiteSpace(employee.Email))
                    return BadRequest("Nhân viên chưa có email.");

                var subject = $"[THÔNG BÁO] Yêu cầu chỉnh sửa bảng lương tháng {payroll.ThangNam.Month}/{payroll.ThangNam.Year}";
                var body = $"Xin chào <b>{employee.HoTen}</b>,<br><br>" +
                           $"Yêu cầu chỉnh sửa bảng lương tháng <b>{payroll.ThangNam.Month}/{payroll.ThangNam.Year}</b> của bạn đã bị từ chối.<br>" +
                           $"Lý do: {action.Reason}<br>" +
                           $"Vui lòng liên hệ phòng nhân sự nếu có thắc mắc.<br><br>Trân trọng.";

                try
                {
                    _emailService.SendEmail(employee.Email, subject, body);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Lỗi gửi email: {ex.Message}");
                }

                payroll.TrangThai = "BL1"; // Quay lại trạng thái chờ nhân viên xác nhận
                payroll.GhiChu = action.Reason;
            }
            else
            {
                return BadRequest("Hành động không hợp lệ. Chỉ hỗ trợ APPROVE hoặc REJECT.");
            }

            await _context.SaveChangesAsync();
            return Ok($"Yêu cầu chỉnh sửa bảng lương đã được xử lý thành công ({action.Action}).");
        }

        // Xử lý bảng lương trả về từ giám đốc
        [HttpPost("ResendToDirector/{maLuong}")]
        public async Task<IActionResult> ResendToDirector(int maLuong, [FromBody] string adjustmentNote)
        {
            var payroll = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .FirstOrDefaultAsync(b => b.MaLuong == maLuong && b.TrangThai == "BL3R");

            if (payroll == null)
                return BadRequest("Bảng lương không tồn tại hoặc không ở trạng thái trả về từ giám đốc (BL3R).");

            if (string.IsNullOrWhiteSpace(adjustmentNote))
                return BadRequest("Ghi chú chỉnh sửa là bắt buộc.");

            // Cập nhật bảng lương
            payroll.TrangThai = "BL3"; // Chuyển lại trạng thái chờ giám đốc duyệt
            payroll.GhiChu = adjustmentNote;
            payroll.NguoiDuyetKT = User.Identity?.Name;
            payroll.NgayDuyetKT = DateTime.Now;
            payroll.NguoiTraVeGD = null;
            payroll.NgayTraVeTuGD = null;

            await _context.SaveChangesAsync();
            return Ok("Bảng lương đã được gửi lại cho giám đốc thành công.");
        }

        // Duyệt bảng lương
        [HttpPost("Approve")]
        public async Task<IActionResult> Approve([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL2")
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một số bảng lương không ở trạng thái phù hợp để duyệt.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3";
                b.NguoiDuyetKT = currentUser;
                b.NgayDuyetKT = currentTime;
            }

            await _context.SaveChangesAsync();
            return Ok("Bảng lương đã được duyệt thành công.");
        }

        // Trả về cho quản lý
        [HttpPost("ReturnToManager")]
        public async Task<IActionResult> ReturnToManager([FromBody] int[] maLuongList, [FromQuery] string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
                return BadRequest("Lý do trả về là bắt buộc.");

            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL2")
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một số bảng lương không ở trạng thái phù hợp để trả về.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL2R";
                b.GhiChu = lyDo;
                b.NguoiTraVeKT = currentUser;
                b.NgayTraVeTuKT = currentTime;
            }

            await _context.SaveChangesAsync();
            return Ok("Bảng lương đã được trả về cho quản lý thành công.");
        }
    }
}