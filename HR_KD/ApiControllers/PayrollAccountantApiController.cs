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
    public class PayrollAccountantApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly Dictionary<string, string> _statusNameMapping;

        public PayrollAccountantApiController(HrDbContext context)
        {
            _context = context;
            _statusNameMapping = _context.TrangThais.ToDictionary(t => t.MaTrangThai, t => t.TenTrangThai);
        }

        #region API cho Kế toán
        [HttpGet("GetPayrolls")] 
        public async Task<IActionResult> GetPayrolls(int year, int month)
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
                        TenTrangThai = _statusNameMapping.TryGetValue(b.TrangThai, out var name) ? name : b.TrangThai
                    })
                });

            return Ok(result);
        }

        [HttpPost("Approve")] 
        public async Task<IActionResult> Approve([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
               .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL2") 
               .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("One or more payrolls are not in the appropriate state for approval.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3"; 
                b.NguoiDuyetKT = currentUser; 
                b.NgayDuyetKT = currentTime; 
            }

            await _context.SaveChangesAsync();
            return Ok("Payrolls approved by Accountant successfully.");
        }

        [HttpPost("ReturnToManager")]
        public async Task<IActionResult> ReturnToManager([FromBody] int[] maLuongList, [FromQuery] string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
                return BadRequest("Reason for return is required.");

            var luongs = await _context.BangLuongs
                .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL2") // Only return if in "Sent to Accountant" state
                .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("One or more payrolls are not in the appropriate state to be returned.");

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
            return Ok("Payrolls returned to Manager successfully.");
        }

        [HttpPost("SendToDirector")] 
        public async Task<IActionResult> SendToDirector([FromBody] int[] maLuongList)
        {
            var luongs = await _context.BangLuongs
               .Where(b => maLuongList.Contains(b.MaLuong) && b.TrangThai == "BL3") // Only send if approved by Accountant
               .ToListAsync();

            if (luongs.Count != maLuongList.Length)
                return BadRequest("One or more payrolls are not in the appropriate state to be sent to Director.");

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.Now;

            foreach (var b in luongs)
            {
                b.TrangThai = "BL3"; 
            }

            await _context.SaveChangesAsync();
            return Ok("Payrolls sent to Director successfully.");
        }

        #endregion
    }
}