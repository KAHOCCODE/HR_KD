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

        private int? GetMaNvFromClaims()
        {
            var maNvClaim = User.FindFirst("MaNV")?.Value;
            return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
        }

        [HttpPost]
        [Route("SubmitLeave")]
        public async Task<IActionResult> SubmitLeave([FromBody] List<LeaveRequestDto> leaveRequests)
        {
            // Lấy mã NV từ claims
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            if (leaveRequests == null || !leaveRequests.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu nghỉ phép không hợp lệ." });
            }

            try
            {
                foreach (var request in leaveRequests)
                {
                    // Validate dữ liệu
                    if (!DateTime.TryParse(request.NgayNghi, out DateTime ngayNghi))
                    {
                        return BadRequest(new { success = false, message = $"Ngày nghỉ không hợp lệ: {request.NgayNghi}" });
                    }

                    if (request.MaLoaiNgayNghi == null || request.MaLoaiNgayNghi <= 0)
                    {
                        return BadRequest(new { success = false, message = "Mã loại ngày nghỉ không hợp lệ." });
                    }

                    // Tạo bản ghi nghỉ phép
                    var leave = new NgayNghi
                    {
                        MaNv = currentMaNv.Value, // Sử dụng mã NV từ claims
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
        public async Task<IActionResult> GetLeaveHistory()
        {
            // Lấy mã NV từ claims
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            var leaveHistory = await _context.NgayNghis
                .Where(n => n.MaNv == currentMaNv.Value) // Sử dụng mã NV từ claims
                .Join(_context.LoaiNgayNghis,
                      n => n.MaLoaiNgayNghi,
                      l => l.MaLoaiNgayNghi,
                      (n, l) => new
                      {
                          n.MaLoaiNgayNghi,
                          TenLoai = l.TenLoai,
                          NgayNghi = n.NgayNghi1.ToString("yyyy-MM-dd"),
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
                    maLoaiNgayNghi = l.MaLoaiNgayNghi, // Đảm bảo tên đúng với frontend
                    tenLoai = l.TenLoai
                })
                .ToListAsync();

            return Ok(new { success = true, leaveTypes });
        }



        [HttpGet("SoNgayConLai")]
        public async Task<IActionResult> GetSoNgayConLai()
        {
            var maNv = GetMaNvFromClaims();
            if (maNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.MaNv == maNv.Value);
            if (nhanVien == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy nhân viên." });
            }

            if (!nhanVien.NgayVaoLam.HasValue)
            {
                return BadRequest(new { success = false, message = "Ngày vào làm không hợp lệ." });
            }

            var soNamLamViec = DateTime.Now.Year - nhanVien.NgayVaoLam.Value.Year;
            if (DateOnly.FromDateTime(DateTime.Now) < nhanVien.NgayVaoLam.Value.AddYears(soNamLamViec))
            {
                soNamLamViec--;
            }

            var soNgayPhepCongThem = 0;
            if (soNamLamViec >= 5)
            {
                var soDot5Nam = soNamLamViec / 5;
                soNgayPhepCongThem = soDot5Nam * 2;
            }

            var currentYear = DateTime.Now.Year;

            var soDuPhep = await _context.SoDuPheps
                .FirstOrDefaultAsync(sd => sd.MaNv == maNv.Value && sd.Nam == currentYear);

            if (soDuPhep == null)
            {
                // Chỉ tạo nếu chưa có bản ghi
                soDuPhep = new SoDuPhep
                {
                    MaNv = maNv.Value,
                    Nam = currentYear,
                    SoNgayConLai = 12 + soNgayPhepCongThem,
                    NgayCapNhat = DateTime.Now
                };

                _context.SoDuPheps.Add(soDuPhep);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                soNgayConLai = soDuPhep?.SoNgayConLai,
                maNv = maNv,
                nam = currentYear,
                soDuTonTai = soDuPhep != null
            });
        }



        public class LeaveRequestDto
        {
            public string NgayNghi { get; set; }
            public int? MaLoaiNgayNghi { get; set; }
            public string LyDo { get; set; }
            // Đã remove property MaNv
        }

    }
}
