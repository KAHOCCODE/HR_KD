using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        public async Task<IActionResult> SubmitLeave([FromForm] string leaveRequestsJson, [FromForm] List<IFormFile> files)
        {
            // Lấy mã NV từ claims
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            // Chuyển đổi JSON string thành List<LeaveRequestDto>
            List<LeaveRequestDto> leaveRequests;
            try
            {
                leaveRequests = JsonSerializer.Deserialize<List<LeaveRequestDto>>(leaveRequestsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            if (leaveRequests == null || !leaveRequests.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu nghỉ phép không hợp lệ." });
            }

            try
            {
                // Xử lý file đính kèm
                string fileAttachments = string.Empty;
                if (files != null && files.Count > 0)
                {
                    var fileNames = new List<string>();

                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    Directory.CreateDirectory(uploadPath); // Đảm bảo thư mục uploads tồn tại

                    foreach (var file in files)
                    {
                        if (file.Length > 0)
                        {
                            string originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                            string extension = Path.GetExtension(file.FileName);
                            string sanitizedFileName = originalFileName + extension;
                            string filePath = Path.Combine(uploadPath, sanitizedFileName);

                            int count = 1;
                            while (System.IO.File.Exists(filePath))
                            {
                                sanitizedFileName = $"{originalFileName}({count}){extension}";
                                filePath = Path.Combine(uploadPath, sanitizedFileName);
                                count++;
                            }

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            fileNames.Add(sanitizedFileName);
                        }
                    }

                    // Nối các tên file bằng dấu gạch ngang
                    fileAttachments = string.Join("-", fileNames);
                }

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
                        NgayLamDon =DateTime.Now,
                        LyDo = request.LyDo ?? "Không có lý do",
                        MaTrangThai = "NN1",
                        MaLoaiNgayNghi = request.MaLoaiNgayNghi.Value,
                        FileDinhKem = fileAttachments // Thêm file đính kèm
                   
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
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            var leaveHistory = await _context.NgayNghis
                .Where(n => n.MaNv == currentMaNv.Value)
                .Join(_context.LoaiNgayNghis,
                      n => n.MaLoaiNgayNghi,
                      l => l.MaLoaiNgayNghi,
                      (n, l) => new { n, l })
                .GroupJoin(_context.NhanViens,
                           nl => nl.n.NguoiDuyetId,
                           nv => nv.MaNv,
                           (nl, nvGroup) => new { nl.n, nl.l, nvGroup })
                .SelectMany(nl => nl.nvGroup.DefaultIfEmpty(),
                            (nl, nv) => new
                            {
                                id = nl.n.MaNgayNghi,
                                MaLoaiNgayNghi = nl.n.MaLoaiNgayNghi,
                                TenLoai = nl.l.TenLoai,
                                NgayNghi = nl.n.NgayNghi1.ToString("yyyy-MM-dd"),
                                LyDo = nl.n.LyDo,
                           
                                TrangThai = _context.TrangThais
                                   .Where(t => t.MaTrangThai == nl.n.MaTrangThai)
                                  .Select(t => t.TenTrangThai)
                                    .FirstOrDefault() ?? "Không xác định",
                                FileDinhKem = nl.n.FileDinhKem,
                                NgayDuyet = nl.n.NgayDuyet,
                                GhiChu = nl.n.GhiChu,
                                NguoiDuyetId = nl.n.NguoiDuyetId,
                                NguoiDuyetHoTen = nv != null ? nv.HoTen : "Chưa có"
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

            var currentYear = DateTime.Now.Year;

            // Lấy số dư phép từ bảng SoDuPhep
            var soDuPhep = await _context.SoDuPheps
                .FirstOrDefaultAsync(sd => sd.MaNv == maNv.Value && sd.Nam == currentYear);

            // Lấy số ngày phép được cấp và số ngày đã sử dụng từ bảng PhepNamNhanVien
            var phepNam = await _context.PhepNamNhanViens
                .FirstOrDefaultAsync(p => p.MaNv == maNv.Value && p.Nam == currentYear);

            if (soDuPhep == null || phepNam == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy dữ liệu phép cho năm hiện tại." });
            }

            return Ok(new
            {
                success = true,
                soNgayConLai = soDuPhep.SoNgayConLai,
                soNgayPhepDuocCap = phepNam.SoNgayPhepDuocCap,
                soNgayDaSuDung = phepNam.SoNgayDaSuDung,
                maNv = maNv,
                nam = currentYear
            });
        }

        [HttpGet]
        [Route("GetAlreadyRegisteredDates")]
        public async Task<IActionResult> GetAlreadyRegisteredDates()
        {
            try
            {
                // Lấy mã NV từ claims
                var currentMaNv = GetMaNvFromClaims();
                if (currentMaNv == null)
                {
                    return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
                }

                // Lấy các ngày đã đăng ký nghỉ phép (chỉ lấy những ngày đang chờ duyệt hoặc đã duyệt)
                var registeredDates = await _context.NgayNghis
                    .Where(n => n.MaNv == currentMaNv.Value &&
                          (n.MaTrangThai == "NN1" || n.MaTrangThai == "NN2"))
                    .Select(n => n.NgayNghi1.ToString("yyyy-MM-dd"))
                    .ToListAsync();

                return Ok(new { success = true, dates = registeredDates });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Server: {ex}");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpGet("GetHolidays")]
        public async Task<IActionResult> GetHolidays()
        {
            try
            {
                var holidays = await _context.NgayLes
                    .Where(h => h.TrangThai == "Đã duyệt")
                    .Select(h => new
                    {
                        ngayLe = h.NgayLe1.ToString("yyyy-MM-dd"),
                        tenNgayLe = h.TenNgayLe,
                        soNgayNghi = h.SoNgayNghi,
                        trangThai = h.TrangThai
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    holidays = holidays
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + ex.Message
                });
            }
        }




        [HttpPatch]
        [Route("CancelLeave/{maNgayNghi}")]
        public async Task<IActionResult> CancelLeave(int maNgayNghi)
        {
            // Lấy mã NV từ claims
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            try
            {
                // Tìm đơn nghỉ phép với maNgayNghi và mã nhân viên tương ứng
                var leaveRequest = await _context.NgayNghis
                    .FirstOrDefaultAsync(n => n.MaNgayNghi == maNgayNghi && n.MaNv == currentMaNv.Value);

                if (leaveRequest == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });
                }

                // Kiểm tra xem có thể hủy không (chỉ hủy được đơn đang chờ duyệt)
                if (leaveRequest.MaTrangThai != "NN1")
                {
                    return BadRequest(new { success = false, message = "Chỉ có thể hủy đơn đang chờ duyệt." });
                }

                // Cập nhật trạng thái thành "Đã hủy" (NN4)
                leaveRequest.MaTrangThai = "NN4";
                leaveRequest.NgayDuyet = DateTime.Now;
                leaveRequest.GhiChu = "Đơn đã được hủy bởi người đăng ký";

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Hủy đơn nghỉ phép thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Server: {ex}");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
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
