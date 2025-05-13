using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
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
    public class PayrollManagerApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly Dictionary<string, string> _statusNameMapping;

        public PayrollManagerApiController(HrDbContext context)
        {
            _context = context;
            _statusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
        }

        #region API cho Trưởng phòng
        [HttpGet("GetDepartmentPayrolls")] 
        public async Task<IActionResult> GetDepartmentPayrolls(int year, int month)
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
                TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
            });

            return Ok(result);
        }

        [HttpPost("SendToAccountant")]
        public async Task<IActionResult> SendToAccountant([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
               .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL1A") 
               .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("One or more payrolls are not in the appropriate state to be sent.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL2"; // BL2: Sent to Accountant for approval
                b.NguoiGuiKT = currentUser;
                b.NgayGuiKT = currentTime; 
            }

            await _context.SaveChangesAsync();
            // Consider adding logging here
            return Ok("Payrolls sent to Accountant successfully.");
        }
        #endregion
    }
}