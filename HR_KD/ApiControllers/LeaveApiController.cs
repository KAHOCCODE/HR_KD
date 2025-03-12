using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    [Route("api/Leave")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private readonly HrDbContext _context;

        public LeaveController(HrDbContext context)
        {
            _context = context;
        }

        // đăng ký nghỉ phép
        [HttpPost]
        [Route("SubmitLeave")]
        public async Task<IActionResult> SubmitLeave([FromBody] List<LeaveRequestDto> leaveRequests)
        {
            if (leaveRequests == null || !leaveRequests.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu nghỉ phép không hợp lệ." });
            }

            try
            {
                Console.WriteLine($"📩 Dữ liệu nhận được: {System.Text.Json.JsonSerializer.Serialize(leaveRequests)}");

                foreach (var request in leaveRequests)
                {
                    if (!DateTime.TryParse(request.NgayNghi, out DateTime ngayNghi))
                    {
                        return BadRequest(new { success = false, message = $"Ngày nghỉ không hợp lệ: {request.NgayNghi}" });
                    }

                    if (request.MaLoaiNgayNghi == null || request.MaLoaiNgayNghi <= 0)
                    {
                        return BadRequest(new { success = false, message = "Mã loại ngày nghỉ không hợp lệ." });
                    }

                    var leave = new NgayNghi
                    {
                        MaNv = request.MaNv,
                        NgayNghi1 = DateOnly.FromDateTime(ngayNghi),
                        LyDo = request.LyDo ?? "Không có lý do",
                        TrangThai = "Chờ duyệt",
                        MaLoaiNgayNghi = request.MaLoaiNgayNghi.Value
                    };

                    _context.NgayNghis.Add(leave);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Đăng ký nghỉ phép thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Server: {ex}");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetLeaveHistory")]
        public async Task<IActionResult> GetLeaveHistory(int maNv)
        {
            var leaveHistory = await _context.NgayNghis
                .Where(n => n.MaNv == maNv)
                .Join(_context.LoaiNgayNghis,
                      n => n.MaLoaiNgayNghi,
                      l => l.MaLoaiNgayNghi,
                      (n, l) => new
                      {
                          n.MaLoaiNgayNghi,
                          TenLoai = l.TenLoai,
                          NgayNghi = n.NgayNghi1.ToString("yyyy-MM-dd"), // ✅ Chuyển về YYYY-MM-DD
                          n.LyDo,
                          n.TrangThai
                      })
                .ToListAsync();

            return Ok(new { success = true, leaveHistory });
        }


        // APi loại nghỉ phép
        [HttpGet]
        [Route("GetLeaveTypes")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var leaveTypes = await _context.LoaiNgayNghis
                .Select(l => new
                {
                    l.MaLoaiNgayNghi,
                    l.TenLoai
                })
                .ToListAsync();

            return Ok(new { success = true, leaveTypes });
        }


        // DTO dùng để nhận dữ liệu từ frontend
        public class LeaveRequestDto
        {
            public int MaNv { get; set; } = 1; // Mặc định là 1
            public string NgayNghi { get; set; }
            public string? LyDo { get; set; }
            public int? MaLoaiNgayNghi { get; set; }
        }

    }
}
