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
            // Lấy MaNV từ claims
            var maNv = GetMaNvFromClaims();
            if (maNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            // Lấy thông tin nhân viên (NgayVaoLam)
            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.MaNv == maNv.Value);
            if (nhanVien == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy nhân viên." });
            }

            // Kiểm tra nhanVien.NgayVaoLam có giá trị hay không
            if (!nhanVien.NgayVaoLam.HasValue)
            {
                return BadRequest(new { success = false, message = "Ngày vào làm không hợp lệ." });
            }

            // Tính số năm làm việc
            var soNamLamViec = DateTime.Now.Year - nhanVien.NgayVaoLam.Value.Year;
            if (DateOnly.FromDateTime(DateTime.Now) < nhanVien.NgayVaoLam.Value.AddYears(soNamLamViec))
            {
                soNamLamViec--;
            }

            // Tính số ngày phép được cộng thêm theo luật lao động và luật công ty
            var soNgayPhepCongThem = 0;
            if (soNamLamViec >= 5)
            {
                var soDot5Nam = soNamLamViec / 5;
                soNgayPhepCongThem = soDot5Nam * 2; // Mỗi 5 năm được +2 ngày (1 lao động + 1 công ty)
            }

            // Lấy thông tin SoNgayConLai từ bảng SoDuPhep (lấy bản ghi có NgayCapNhat gần nhất)
            var soDuPhep = await _context.SoDuPheps
                .Where(sd => sd.MaNv == maNv.Value && sd.Nam == DateTime.Now.Year)
                .OrderByDescending(sd => sd.NgayCapNhat)
                .FirstOrDefaultAsync();

            // Nếu không có bản ghi, tạo mới với SoNgayConLai mặc định là 12 + số ngày phép cộng thêm
            if (soDuPhep == null)
            {
                // Nếu chưa có bản ghi => tạo mới
                soDuPhep = new SoDuPhep
                {
                    MaNv = maNv.Value,
                    Nam = DateTime.Now.Year,
                    SoNgayConLai = 12 + soNgayPhepCongThem,
                    NgayCapNhat = DateTime.Now
                };
                _context.SoDuPheps.Add(soDuPhep);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Nếu đã có bản ghi => kiểm tra và cộng thêm ngày phép nếu chưa đủ
                int soNgayPhepDuKien = 12 + soNgayPhepCongThem;
                if (soDuPhep.SoNgayConLai < soNgayPhepDuKien)
                {
                    soDuPhep.SoNgayConLai = soNgayPhepDuKien;
                    soDuPhep.NgayCapNhat = DateTime.Now;
                    _context.SoDuPheps.Update(soDuPhep);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { success = true, soNgayConLai = soDuPhep.SoNgayConLai });
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
