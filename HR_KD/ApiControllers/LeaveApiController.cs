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
                        MaTrangThai = 1,
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
                                // Thay MaTrangThai bằng TenTrangThai
                                //TrangThai = _context.TrangThais
                                //    .Where(t => t.MaTrangThai == nl.n.MaTrangThai)
                                //    .Select(t => t.TenTrangThai)
                                //    .FirstOrDefault() ?? "Không xác định",
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

            if (!nhanVien.NgayVaoLam.HasValue)
            {
                return BadRequest(new { success = false, message = "Ngày vào làm không hợp lệ." });
            }

            var currentDate = DateTime.Now;
            var currentYear = currentDate.Year;

            // Tính số năm làm việc
            var soNamLamViec = currentYear - nhanVien.NgayVaoLam.Value.Year;
            if (DateOnly.FromDateTime(currentDate) < DateOnly.FromDateTime(new DateTime(currentYear, nhanVien.NgayVaoLam.Value.Month, nhanVien.NgayVaoLam.Value.Day)))
            {
                soNamLamViec--;
            }

            // Số ngày phép mặc định
            int soNgayPhepMacDinh = 12;

            // Tính số ngày phép bổ sung
            int soNgayPhepBoSung = 0;

            // Luật Lao động: Làm đủ 5 năm liên tục, tăng 1 ngày phép
            if (soNamLamViec >= 5)
            {
                soNgayPhepBoSung += 1;
            }

            // Luật công ty: Cứ 5 năm tăng 1 ngày phép
            int soDot5Nam = soNamLamViec / 5;
            soNgayPhepBoSung += soDot5Nam;

            // Tổng số ngày phép
            int tongSoNgayPhep = soNgayPhepMacDinh + soNgayPhepBoSung;

            // Kiểm tra và cập nhật số dư phép cho năm hiện tại
            var soDuPhep = await _context.SoDuPheps
                .FirstOrDefaultAsync(sd => sd.MaNv == maNv.Value && sd.Nam == currentYear);

            if (soDuPhep == null)
            {
                // Tạo mới nếu chưa có bản ghi cho năm hiện tại
                soDuPhep = new SoDuPhep
                {
                    MaNv = maNv.Value,
                    Nam = currentYear,
                    SoNgayConLai = tongSoNgayPhep,
                    NgayCapNhat = currentDate
                };
                _context.SoDuPheps.Add(soDuPhep);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                soNgayConLai = soDuPhep.SoNgayConLai,
                maNv = maNv,
                nam = currentYear,
                soNamLamViec = soNamLamViec,
                ngayPhepMacDinh = soNgayPhepMacDinh,
                ngayPhepBoSung = soNgayPhepBoSung
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
                          (n.MaTrangThai == 1 || n.MaTrangThai == 2))
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


        [HttpDelete]
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
                if (leaveRequest.MaTrangThai != 1)
                {
                    return BadRequest(new { success = false, message = "Chỉ có thể hủy đơn đang chờ duyệt." });
                }

                // Xóa đơn nghỉ phép
                _context.NgayNghis.Remove(leaveRequest);
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
