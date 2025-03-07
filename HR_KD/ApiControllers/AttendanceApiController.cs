using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{ 
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly HrDbContext _context;

        public AttendanceController(HrDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        public IActionResult PostAttendance([FromBody] List<ChamCong> attendanceData)
        {
            if (attendanceData == null || attendanceData.Count == 0)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            // Xử lý logic lưu dữ liệu vào database

            return Ok(new { success = true, message = "Chấm công thành công!" });
        }
        [HttpPost]
        [Route("SubmitAttendance")]
        public async Task<IActionResult> SubmitAttendance([FromBody] List<ChamCong> attendanceData)
        {
            if (attendanceData == null || !attendanceData.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
            }

            try
            {
                foreach (var entry in attendanceData)
                {
                    var chamCong = new ChamCong
                    {
                        MaNv = entry.MaNv, // Lấy mã nhân viên từ request thay vì cố định
                        NgayLamViec = entry.NgayLamViec,
                        GioVao = entry.GioVao, // Có thể là null
                        GioRa = entry.GioRa,   // Có thể là null
                        TongGio = entry.TongGio ?? 0, // Nếu null thì gán 0
                        TrangThai = "0", // Mặc định trạng thái = 0
                        GhiChu = entry.GhiChu
                    };
                    _context.ChamCongs.Add(chamCong);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Chấm công thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }
    }
}
