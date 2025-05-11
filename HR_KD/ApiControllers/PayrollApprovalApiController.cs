using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollApprovalApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private Dictionary<string, string> _StatusNameMapping;
        public PayrollApprovalApiController(HrDbContext context)
        {
            _context = context;
            _StatusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
        }

        #region API cho Trưởng phòng
        [HttpGet("GetDepartmentPayrollsForManager")]
        public async Task<IActionResult> GetDepartmentPayrollsForManager(int year, int month)
        {
            var username = User.Identity?.Name;
            var manager = await _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .FirstOrDefaultAsync(t => t.Username == username);

            if (manager?.MaNvNavigation?.MaPhongBanNavigation == null)
                return Unauthorized("Bạn không có quyền xem bảng lương phòng ban.");

            var maPhongBan = manager.MaNvNavigation.MaPhongBanNavigation.MaPhongBan;

            var bangLuongs = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.MaNvNavigation.MaPhongBan == maPhongBan)
                .ToListAsync();

            var result = bangLuongs.Select(b => new
            {
                b.MaLuong,
                b.MaNv,
                HoTen = b.MaNvNavigation.HoTen,
                b.ThucNhan,
                b.TrangThai,
                TenTrangThai = _StatusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
            });

            return Ok(result);
        }

        [HttpPost("SendToAccountant")]
        public async Task<IActionResult> SendToAccountant([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL1A") // Chỉ gửi khi đã được nhân viên xác nhận
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái phù hợp để gửi.");

            foreach (var b in luongs)
            {
                b.TrangThai = "BL2"; // BL2: Gửi kế toán duyệt
            }

            await _context.SaveChangesAsync();
            return Ok("Đã gửi bảng lương đến kế toán.");
        }
        #endregion

        #region API cho Kế toán
        [HttpGet("GetPayrollsForAccountant")]
        public async Task<IActionResult> GetPayrollsForAccountant(int year, int month)
        {
            var bangLuongs = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.TrangThai == "BL2")
                .ToListAsync();

            var result = bangLuongs.GroupBy(b => b.MaNvNavigation.MaPhongBanNavigation.TenPhongBan)
                .Select(g => new
                {
                    TenPhongBan = g.Key,
                    Payrolls = g.Select(b => new
                    {
                        b.MaLuong,
                        b.MaNv,
                        HoTen = b.MaNvNavigation.HoTen,
                        b.ThucNhan,
                        b.TrangThai,
                        TenTrangThai = _StatusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
                    })
                });

            return Ok(result);
        }

        [HttpPost("ApproveByAccountant")]
        public async Task<IActionResult> ApproveByAccountant([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL2") // Chỉ duyệt khi ở trạng thái "Gửi kế toán"
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái phù hợp để duyệt.");

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3"; // BL3: Kế toán đã duyệt
                // TODO: Lưu thông tin kế toán duyệt + thời gian duyệt
            }

            await _context.SaveChangesAsync();
            return Ok("Kế toán đã duyệt.");
        }

        [HttpPost("ReturnToManager")]
        public async Task<IActionResult> ReturnToManager([FromBody] int[] maLuongList, [FromQuery] string lyDo)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL2") 
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái phù hợp để trả về.");

            foreach (var b in luongs)
            {
                b.TrangThai = "BL2R"; // BL2R: Kế toán trả về
                b.GhiChu = lyDo;
            }

            await _context.SaveChangesAsync();
            return Ok("Đã trả về cho trưởng phòng.");
        }
        #endregion

        #region API cho Giám đốc
        [HttpGet("GetPayrollsForDirector")]
        public async Task<IActionResult> GetPayrollsForDirector(int year, int month)
        {
            var bangLuongs = await _context.BangLuongs
                .Include(b => b.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Where(b => b.ThangNam.Year == year && b.ThangNam.Month == month && b.TrangThai == "BL3") // Chỉ xem các bảng lương đã được kế toán duyệt
                .ToListAsync();

            var result = bangLuongs.Select(b => new
            {
                b.MaLuong,
                b.MaNv,
                HoTen = b.MaNvNavigation.HoTen,
                b.ThucNhan,
                b.TrangThai,
                TenTrangThai = _StatusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
            });

            return Ok(result);
        }

        [HttpPost("FinalApprove")]
        public async Task<IActionResult> FinalApprove([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3") // Chỉ duyệt khi ở trạng thái "Kế toán duyệt"
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái phù hợp để duyệt.");

            foreach (var b in luongs)
            {
                b.TrangThai = "BL4"; // BL4: Giám đốc duyệt cuối
                // TODO: Lưu thông tin giám đốc duyệt + thời gian duyệt
            }

            await _context.SaveChangesAsync();
            return Ok("Giám đốc đã duyệt cuối.");
        }

        [HttpPost("ReturnToAccountant")]
        public async Task<IActionResult> ReturnToAccountant([FromBody] int[] maLuongList, [FromQuery] string lyDo)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3") // Chỉ trả về khi ở trạng thái "Kế toán duyệt"
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái phù hợp để trả về.");

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3R"; 
                b.GhiChu = lyDo;
            }

            await _context.SaveChangesAsync();
            return Ok("Đã trả về cho kế toán.");
        }
        #endregion

        #region API để gửi bảng lương cho nhân viên (sau khi giám đốc duyệt)
        [HttpPost("SendPayrollToEmployees")]
        public async Task<IActionResult> SendPayrollToEmployees([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL4") 
                .Include(b => b.MaNvNavigation)
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("Một hoặc nhiều bảng lương không ở trạng thái phù hợp để gửi cho nhân viên.");

            foreach (var b in luongs)
            {
                // TODO: Logic xuất PDF bảng lương cho từng nhân viên
                // TODO: Logic gửi email bảng lương đến nhân viên (b.MaNvNavigation.Email)
                b.TrangThai = "BL5"; 
            }

            await _context.SaveChangesAsync();
            return Ok("Đã gửi bảng lương cho nhân viên.");
        }
        #endregion
    }
}