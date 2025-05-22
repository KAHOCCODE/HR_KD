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
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
                    Directory.CreateDirectory(uploadPath);

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

                    fileAttachments = string.Join("-", fileNames);
                }

                // Tạo mã đơn ngẫu nhiên 5 ký tự
                string maDon = await GenerateRandomMaDonAsync();

                // Kiểm tra giới hạn cho từng loại ngày nghỉ
                var maLoaiNgayNghis = leaveRequests.Select(r => r.MaLoaiNgayNghi.Value).Distinct();
                foreach (var maLoai in maLoaiNgayNghis)
                {
                    var loaiNgayNghi = await _context.LoaiNgayNghis
                        .FirstOrDefaultAsync(l => l.MaLoaiNgayNghi == maLoai);
                    if (loaiNgayNghi == null)
                    {
                        return BadRequest(new { success = false, message = $"Loại ngày nghỉ {maLoai} không tồn tại." });
                    }

                    // Số ngày nghỉ trong yêu cầu hiện tại cho loại này
                    var soNgayYeuCau = leaveRequests.Count(r => r.MaLoaiNgayNghi == maLoai);

                    // Kiểm tra số ngày nghỉ tối đa cho mỗi lần đăng ký
                    if (loaiNgayNghi.SoNgayNghiToiDa.HasValue &&
                        soNgayYeuCau > loaiNgayNghi.SoNgayNghiToiDa.Value)
                    {
                        return BadRequest(new { success = false, message = $"Vượt quá số ngày nghỉ tối đa cho mỗi lần đăng ký của loại {loaiNgayNghi.TenLoai}." });
                    }

                    // Đếm số lần đăng ký trong năm (dựa trên MaDon)
                    var soLanDangKy = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == maLoai && 
                               (n.MaTrangThai == "NN1" || n.MaTrangThai == "NN2" || n.MaTrangThai == "NN5") &&
                               n.NgayNghi1.Year == DateTime.Now.Year)
                        .Select(n => n.MaDon)
                        .Distinct()
                        .CountAsync();

                    if (loaiNgayNghi.SoLanDangKyToiDa.HasValue &&
                        (soLanDangKy + 1) > loaiNgayNghi.SoLanDangKyToiDa.Value)
                    {
                        return BadRequest(new { success = false, message = $"Vượt quá số lần đăng ký tối đa cho loại {loaiNgayNghi.TenLoai} trong năm." });
                    }
                }

                // Lưu các ngày nghỉ
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
                        MaNv = currentMaNv.Value,
                        NgayNghi1 = DateOnly.FromDateTime(ngayNghi),
                        NgayLamDon = DateTime.Now,
                        LyDo = request.LyDo ?? "Không có lý do",
                        MaTrangThai = "NN1",
                        MaLoaiNgayNghi = request.MaLoaiNgayNghi.Value,
                        FileDinhKem = fileAttachments,
                        MaDon = maDon // Gán mã đơn
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

        // Hàm sinh chuỗi ngẫu nhiên 5 ký tự
        private async Task<string> GenerateRandomMaDonAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            string maDon;

            do
            {
                maDon = new string(Enumerable.Repeat(chars, 5)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (await _context.NgayNghis.AnyAsync(n => n.MaDon == maDon)); // Kiểm tra trùng lặp

            return maDon;
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
                                MaDon = nl.n.MaDon,
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
                                NguoiDuyetHoTen = nv != null ? nv.HoTen : "Chưa có",
                                LyDoTuChoi = nl.n.LyDoTuChoi,
                                LyDoHuy = nl.n.LyDoHuy
                            })
                .ToListAsync();

            // Nhóm các đơn theo MaDon
            var groupedLeaveHistory = leaveHistory
                .GroupBy(x => x.MaDon)
                .Select(g => new
                {
                    MaDon = g.Key,
                    TenLoai = g.First().TenLoai,
                    LyDo = g.First().LyDo,
                    TrangThai = g.First().TrangThai,
                    FileDinhKem = g.First().FileDinhKem,
                    NgayDuyet = g.First().NgayDuyet,
                    GhiChu = g.First().GhiChu,
                    NguoiDuyetHoTen = g.First().NguoiDuyetHoTen,
                    LyDoTuChoi = g.First().LyDoTuChoi,
                    LyDoHuy = g.First().LyDoHuy,
                    NgayNghis = g.Select(x => new
                    {
                        id = x.id,
                        NgayNghi = x.NgayNghi
                    }).ToList()
                })
                .ToList();

            return Ok(new { success = true, leaveHistory = groupedLeaveHistory });
        }


        // APi loại nghỉ phép
        [HttpGet]
        [Route("GetLeaveTypes")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var leaveTypes = await _context.LoaiNgayNghis
                .Select(l => new
                {
                    maLoaiNgayNghi = l.MaLoaiNgayNghi,
                    tenLoai = l.TenLoai,
                    moTa = l.MoTa
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

                // Lấy các ngày đã đăng ký nghỉ phép (chỉ lấy những ngày đang chờ duyệt, đã duyệt, NN5 hoặc NN6)
                var registeredDates = await _context.NgayNghis
                    .Where(n => n.MaNv == currentMaNv.Value &&
                          (n.MaTrangThai == "NN1" || n.MaTrangThai == "NN2" ||
                           n.MaTrangThai == "NN5" || n.MaTrangThai == "NN6") &&
                          n.MaTrangThai != "NN4") // Loại bỏ các đơn đã hủy
                    .Select(n => new {
                        date = n.NgayNghi1.ToString("yyyy-MM-dd"),
                        status = n.MaTrangThai == "NN1" ? "pending" :
                                 n.MaTrangThai == "NN2" ? "approved" :
                                 n.MaTrangThai == "NN5" ? "status5" : "status6" // Gán tên trạng thái cho NN5 và NN6
                    })
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
                    .Where(h => h.TrangThai == "NL4")
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
        public async Task<IActionResult> CancelLeave(int maNgayNghi, [FromBody] CancelLeaveRequest request)
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

                // Lấy tất cả các ngày nghỉ trong cùng đơn
                var allLeaveDays = await _context.NgayNghis
                    .Where(n => n.MaDon == leaveRequest.MaDon && n.MaNv == currentMaNv.Value)
                    .ToListAsync();

                // Cập nhật trạng thái thành "Đã hủy" (NN4) cho tất cả các ngày nghỉ trong đơn
                foreach (var leaveDay in allLeaveDays)
                {
                    leaveDay.MaTrangThai = "NN4";
                    leaveDay.NgayDuyet = DateTime.Now;
                    leaveDay.LyDoHuy = request.LyDoHuy;
                    leaveDay.GhiChu = "Đơn được hủy bởi người đăng ký";
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    message = "Hủy đơn nghỉ phép thành công.",
                    shouldReload = true // Thêm flag để client biết cần reload trang
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi Server: {ex}");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("CheckLeaveLimits")]
        public async Task<IActionResult> CheckLeaveLimits(int maLoaiNgayNghi, int soNgayYeuCau)
        {
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            try
            {
                var loaiNgayNghi = await _context.LoaiNgayNghis
                    .FirstOrDefaultAsync(l => l.MaLoaiNgayNghi == maLoaiNgayNghi);
                
                if (loaiNgayNghi == null)
                {
                    return BadRequest(new { success = false, message = "Loại ngày nghỉ không tồn tại." });
                }

                // Kiểm tra số ngày nghỉ tối đa cho mỗi lần đăng ký
                var vuotQuaSoNgay = loaiNgayNghi.SoNgayNghiToiDa.HasValue && 
                                   soNgayYeuCau > loaiNgayNghi.SoNgayNghiToiDa.Value;

                // Kiểm tra số lần đăng ký trong năm
                var soLanDangKy = await _context.NgayNghis
                    .Where(n => n.MaNv == currentMaNv.Value && 
                           n.MaLoaiNgayNghi == maLoaiNgayNghi && 
                           (n.MaTrangThai == "NN1" || n.MaTrangThai == "NN2" || n.MaTrangThai == "NN5") &&
                           n.NgayNghi1.Year == DateTime.Now.Year)
                    .Select(n => n.MaDon)
                    .Distinct()
                    .CountAsync();

                var vuotQuaSoLan = loaiNgayNghi.SoLanDangKyToiDa.HasValue && 
                                  (soLanDangKy + 1) > loaiNgayNghi.SoLanDangKyToiDa.Value;

                var result = new
                {
                    success = true,
                    loaiNgayNghi = new
                    {
                        tenLoai = loaiNgayNghi.TenLoai,
                        soNgayNghiToiDa = loaiNgayNghi.SoNgayNghiToiDa,
                        soLanDangKyToiDa = loaiNgayNghi.SoLanDangKyToiDa
                    },
                    hienTai = new
                    {
                        soLanDangKy = soLanDangKy
                    },
                    yeuCau = new
                    {
                        soNgayYeuCau = soNgayYeuCau
                    },
                    kiemTra = new
                    {
                        vuotQuaSoNgay,
                        vuotQuaSoLan,
                        biVoHieuHoa = vuotQuaSoLan
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetLeaveTypeLimits")]
        public async Task<IActionResult> GetLeaveTypeLimits()
        {
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            try
            {
                var leaveTypes = await _context.LoaiNgayNghis
                    .Select(l => new
                    {
                        maLoaiNgayNghi = l.MaLoaiNgayNghi,
                        tenLoai = l.TenLoai,
                        soNgayNghiToiDa = l.SoNgayNghiToiDa,
                        soLanDangKyToiDa = l.SoLanDangKyToiDa
                    })
                    .ToListAsync();

                var result = new List<object>();

                foreach (var leaveType in leaveTypes)
                {
                    // Đếm số ngày nghỉ đã đăng ký (NN1)
                    var soNgayDaDangKy = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == leaveType.maLoaiNgayNghi && 
                               n.MaTrangThai == "NN1")
                        .CountAsync();

                    // Đếm số ngày nghỉ đã duyệt (NN2)
                    var soNgayDaDuyet = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == leaveType.maLoaiNgayNghi && 
                               n.MaTrangThai == "NN2")
                        .CountAsync();

                    // Đếm số ngày nghỉ không lương đã duyệt (NN5)
                    var soNgayKhongLuong = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == leaveType.maLoaiNgayNghi && 
                               n.MaTrangThai == "NN5")
                        .CountAsync();

                    // Đếm số lần đăng ký đang chờ duyệt (NN1)
                    var soLanDangKy = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == leaveType.maLoaiNgayNghi && 
                               n.MaTrangThai == "NN1" &&
                               n.NgayNghi1.Year == DateTime.Now.Year)
                        .Select(n => n.MaDon)
                        .Distinct()
                        .CountAsync();

                    // Đếm số lần đăng ký đã duyệt (NN2)
                    var soLanDaDuyet = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == leaveType.maLoaiNgayNghi && 
                               n.MaTrangThai == "NN2" &&
                               n.NgayNghi1.Year == DateTime.Now.Year)
                        .Select(n => n.MaDon)
                        .Distinct()
                        .CountAsync();

                    // Đếm số lần đăng ký không lương đã duyệt (NN5)
                    var soLanKhongLuong = await _context.NgayNghis
                        .Where(n => n.MaNv == currentMaNv.Value && 
                               n.MaLoaiNgayNghi == leaveType.maLoaiNgayNghi && 
                               n.MaTrangThai == "NN5" &&
                               n.NgayNghi1.Year == DateTime.Now.Year)
                        .Select(n => n.MaDon)
                        .Distinct()
                        .CountAsync();

                    // Kiểm tra xem có vượt quá giới hạn không
                    var vuotQuaSoNgay = leaveType.soNgayNghiToiDa.HasValue && 
                                       (soNgayDaDangKy + soNgayDaDuyet + soNgayKhongLuong) > leaveType.soNgayNghiToiDa.Value;
                    
                    var vuotQuaSoLan = leaveType.soLanDangKyToiDa.HasValue && 
                                      (soLanDangKy + soLanDaDuyet + soLanKhongLuong) >= leaveType.soLanDangKyToiDa.Value;

                    // Vô hiệu hóa loại nghỉ nếu đã đạt đủ số lần đăng ký
                    var biVoHieuHoa = vuotQuaSoLan;

                    result.Add(new
                    {
                        leaveType.maLoaiNgayNghi,
                        leaveType.tenLoai,
                        leaveType.soNgayNghiToiDa,
                        leaveType.soLanDangKyToiDa,
                        hienTai = new
                        {
                            soNgayDaDangKy,
                            soNgayDaDuyet,
                            soNgayKhongLuong,
                            soLanDangKy,
                            soLanDaDuyet,
                            soLanKhongLuong
                        },
                        kiemTra = new
                        {
                            vuotQuaSoNgay,
                            vuotQuaSoLan,
                            biVoHieuHoa = biVoHieuHoa
                        }
                    });
                }

                return Ok(new { success = true, leaveTypes = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", error = ex.Message });
            }
        }
        [HttpPost]
        [Route("SupplementLeave")]
        public async Task<IActionResult> SupplementLeave([FromForm] int leaveId, [FromForm] List<IFormFile> files)
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
                    .FirstOrDefaultAsync(n => n.MaNgayNghi == leaveId && n.MaNv == currentMaNv.Value);

                if (leaveRequest == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đơn nghỉ phép." });
                }

                // Kiểm tra trạng thái đơn (chỉ cho phép bổ sung nếu trạng thái là NN6)
                if (leaveRequest.MaTrangThai != "NN6")
                {
                    return BadRequest(new { success = false, message = "Chỉ có thể bổ sung tài liệu cho đơn đang chờ bổ sung." });
                }

                // Lấy tất cả các ngày nghỉ trong cùng đơn
                var allLeaveDays = await _context.NgayNghis
                    .Where(n => n.MaDon == leaveRequest.MaDon && n.MaNv == currentMaNv.Value)
                    .ToListAsync();

                // Xử lý file đính kèm
                string fileAttachments = leaveRequest.FileDinhKem ?? string.Empty;
                if (files != null && files.Count > 0)
                {
                    var fileNames = new List<string>(fileAttachments.Split('-', StringSplitOptions.RemoveEmptyEntries));
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
                    Directory.CreateDirectory(uploadPath);

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

                    fileAttachments = string.Join("-", fileNames);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Vui lòng cung cấp ít nhất một file bổ sung." });
                }

                // Cập nhật tất cả các ngày nghỉ trong đơn
                foreach (var leaveDay in allLeaveDays)
                {
                    leaveDay.FileDinhKem = fileAttachments;
                    leaveDay.MaTrangThai = "NN1"; // Chuyển trạng thái về Chờ duyệt
                    leaveDay.GhiChu = leaveDay.GhiChu != null
                        ? $"{leaveDay.GhiChu}; Tài liệu bổ sung đã được gửi bởi người đăng ký"
                        : "Tài liệu bổ sung đã được gửi bởi người đăng ký";
                    leaveDay.LyDoTuChoi = null; // Clear the LyDoTuChoi field
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Bổ sung tài liệu thành công." });
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

        public class CancelLeaveRequest
        {
            public string LyDoHuy { get; set; }
        }

    }
}
