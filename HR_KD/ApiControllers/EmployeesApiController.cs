using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.DTOs;
using System.IO;
using System.Collections.Generic;
using HR_KD.Helpers;
using Humanizer;
using HR_KD.Services;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly EmailService _emailService;
        private readonly UsernameGeneratorService _usernameGen;
        private readonly ILogger<EmployeesApiController> _logger;

        public EmployeesApiController(HrDbContext context, EmailService emailService, ILogger<EmployeesApiController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _usernameGen = new UsernameGeneratorService();
        }

        #region Lấy danh sách nhân viên
        [HttpGet]
        public IActionResult GetEmployees()
        {
            var employees = _context.NhanViens
                .Include(e => e.MaPhongBanNavigation)
                .Include(e => e.MaChucVuNavigation)
                .Select(e => new
                {
                    e.MaNv,
                    e.HoTen,
                    e.NgaySinh,
                    GioiTinh = e.GioiTinh.HasValue ? (e.GioiTinh.Value ? "Nam" : "Nữ") : "Không xác định",
                    e.DiaChi,
                    e.Sdt,
                    e.Email,
                    e.TrinhDoHocVan,
                    e.NgayVaoLam,
                    ChucVu = e.MaChucVuNavigation.TenChucVu,
                    PhongBan = e.MaPhongBanNavigation.TenPhongBan,
                    e.AvatarUrl,
                    e.SoNguoiPhuThuoc
                })
                .ToList();
            return Ok(employees);
        }
        #endregion

        #region Lấy danh sách hợp đồng nhân viên
        [HttpGet("GetEmployeeContracts")]
        public IActionResult GetEmployeeContracts()
        {
            var contracts = _context.NhanViens
                .GroupJoin(_context.HopDongLaoDongs,
                    nv => nv.MaNv,
                    hd => hd.MaNv,
                    (nv, hd) => new { NhanVien = nv, HopDong = hd })
                .SelectMany(
                    x => x.HopDong.DefaultIfEmpty(),
                    (nv, hd) => new
                    {
                        MaNv = nv.NhanVien.MaNv,
                        HoTen = nv.NhanVien.HoTen,
                        MaHopDong = hd != null ? hd.MaHopDong : (int?)null,
                        MaLoaiHopDong = hd != null ? hd.MaLoaiHopDong : (int?)null,
                        TenLoaiHopDong = hd != null && hd.LoaiHopDong != null ? hd.LoaiHopDong.TenLoaiHopDong : null,
                        ThoiHan = hd != null ? hd.ThoiHan : null,
                        NgayBatDau = hd != null ? hd.NgayBatDau : null,
                        NgayKetThuc = hd != null ? hd.NgayKetThuc : null,
                        GhiChu = hd != null ? hd.GhiChu : null,
                        SoLanGiaHan = hd != null ? hd.SoLanGiaHan : null,
                        GiaHanToiDa = hd != null && hd.LoaiHopDong != null ? hd.LoaiHopDong.GiaHanToiDa : null
                    })
                .ToList();
            return Ok(contracts);
        }
        #endregion

        #region Lấy hợp đồng của một nhân viên
        [HttpGet("GetEmployeeContract/{maNv}")]
        public IActionResult GetEmployeeContract(int maNv)
        {
            var contract = _context.HopDongLaoDongs
                .Where(hd => hd.MaNv == maNv && hd.IsActive)
                .Select(hd => new
                {
                    hd.MaHopDong,
                    hd.MaLoaiHopDong,
                    hd.ThoiHan,
                    hd.NgayBatDau,
                    hd.NgayKetThuc,
                    hd.GhiChu,
                    hd.SoLanGiaHan
                })
                .FirstOrDefault();

            if (contract == null)
            {
                return Ok(new { });
            }

            return Ok(contract);
        }
        #endregion

        #region Kiểm tra nhân viên đã có thông tin lương hay chưa
        [HttpGet("CheckEmployeeSalary/{maNv}")]
        public IActionResult CheckEmployeeSalary(int maNv)
        {
            try
            {
                // Kiểm tra nhân viên tồn tại
                if (!_context.NhanViens.Any(nv => nv.MaNv == maNv))
                {
                    return BadRequest(new { message = "Nhân viên không tồn tại." });
                }

                // Kiểm tra xem nhân viên đã có thông tin lương hay chưa
                bool hasSalary = _context.BangLuongs.Any(l => l.MaNv == maNv);

                return Ok(new { hasSalary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra thông tin lương của nhân viên.");
                return StatusCode(500, new { message = "Lỗi server. Xem log để biết chi tiết.", error = ex.Message });
            }
        }
        #endregion

        #region Thêm hoặc cập nhật hợp đồng
        [HttpPost("SaveContract")]
        public IActionResult SaveContract([FromBody] ContractDTO contractDto)
        {
            if (contractDto == null)
            {
                return BadRequest(new { message = "Dữ liệu hợp đồng không được để trống." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = string.Join(" | ", errors) });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Kiểm tra MaNv hợp lệ
                if (contractDto.MaNv <= 0)
                {
                    return BadRequest(new { message = "Mã nhân viên không hợp lệ." });
                }

                // Kiểm tra nhân viên tồn tại
                if (!_context.NhanViens.Any(nv => nv.MaNv == contractDto.MaNv))
                {
                    return BadRequest(new { message = "Nhân viên không tồn tại." });
                }

                // Kiểm tra loại hợp đồng hợp lệ
                var loaiHopDong = _context.LoaiHopDongs
                    .FirstOrDefault(l => l.MaLoaiHopDong == contractDto.MaLoaiHopDong);
                if (loaiHopDong == null)
                {
                    return BadRequest(new { message = "Loại hợp đồng không tồn tại." });
                }

                // Kiểm tra NgayBatDau bắt buộc
                if (contractDto.NgayBatDau == default)
                {
                    return BadRequest(new { message = "Ngày bắt đầu là bắt buộc." });
                }

                // Kiểm tra ThoiHan so với ThoiHanMacDinh
                // Bỏ qua kiểm tra nếu là hợp đồng không xác định thời hạn (MaLoaiHopDong = 2)
                if (contractDto.MaLoaiHopDong != 2)
                {
                    if (loaiHopDong.ThoiHanMacDinh.HasValue && contractDto.ThoiHan.HasValue)
                    {
                        if (contractDto.ThoiHan.Value > loaiHopDong.ThoiHanMacDinh.Value)
                        {
                            return BadRequest(new { message = $"Thời hạn hợp đồng không được vượt quá {loaiHopDong.ThoiHanMacDinh.Value} tháng." });
                        }
                    }
                    else if (!loaiHopDong.ThoiHanMacDinh.HasValue && contractDto.ThoiHan.HasValue)
                    {
                        return BadRequest(new { message = "Loại hợp đồng này không cho phép nhập thời hạn." });
                    }
                }
                else
                {
                    // Hợp đồng không xác định thời hạn: đảm bảo ThoiHan và NgayKetThuc là null
                    contractDto.ThoiHan = null;
                    contractDto.NgayKetThuc = null;
                }

                // Tính NgayKetThuc nếu không được gửi từ frontend
                DateOnly? ngayKetThuc = null;
                if (contractDto.MaLoaiHopDong != 2) // Chỉ tính nếu không phải hợp đồng không xác định thời hạn
                {
                    if (!string.IsNullOrEmpty(contractDto.NgayKetThuc?.ToString()))
                    {
                        ngayKetThuc = DateOnly.FromDateTime(contractDto.NgayKetThuc.Value);
                    }
                    else if (contractDto.ThoiHan.HasValue && contractDto.ThoiHan > 0)
                    {
                        ngayKetThuc = DateOnly.FromDateTime(contractDto.NgayBatDau.AddMonths(contractDto.ThoiHan.Value));
                    }
                }

                if (contractDto.MaHopDong > 0)
                {
                    // Cập nhật hợp đồng
                    var existingContract = _context.HopDongLaoDongs
                        .FirstOrDefault(hd => hd.MaHopDong == contractDto.MaHopDong && hd.MaNv == contractDto.MaNv);

                    if (existingContract == null)
                    {
                        return NotFound(new { message = "Hợp đồng không tồn tại." });
                    }

                    existingContract.MaLoaiHopDong = contractDto.MaLoaiHopDong;
                    existingContract.ThoiHan = contractDto.ThoiHan;
                    existingContract.NgayBatDau = DateOnly.FromDateTime(contractDto.NgayBatDau);
                    existingContract.NgayKetThuc = ngayKetThuc;
                    existingContract.GhiChu = contractDto.GhiChu;
                    existingContract.IsActive = true;

                    _context.HopDongLaoDongs.Update(existingContract);
                }
                else
                {
                    // Thêm hợp đồng mới
                    var newContract = new HopDongLaoDong
                    {
                        MaNv = contractDto.MaNv,
                        MaLoaiHopDong = contractDto.MaLoaiHopDong,
                        ThoiHan = contractDto.ThoiHan,
                        NgayBatDau = DateOnly.FromDateTime(contractDto.NgayBatDau),
                        NgayKetThuc = ngayKetThuc,
                        GhiChu = contractDto.GhiChu,
                        SoLanGiaHan = 0,
                        IsActive = true
                    };

                    _context.HopDongLaoDongs.Add(newContract);
                }

                _context.SaveChanges();
                transaction.Commit();

                return Ok(new { message = "Hợp đồng đã được lưu thành công.", maNv = contractDto.MaNv });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Lỗi khi lưu hợp đồng.");
                return StatusCode(500, new { message = "Lỗi server. Xem log để biết chi tiết.", error = ex.Message });
            }
        }
        #endregion

        #region Gia hạn hợp đồng
        [HttpPost("ExtendContract")]
        public IActionResult ExtendContract([FromBody] ExtendContractDTO extendDto)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Kiểm tra hợp đồng tồn tại
                var contract = _context.HopDongLaoDongs
                    .Include(hd => hd.LoaiHopDong)
                    .FirstOrDefault(hd => hd.MaHopDong == int.Parse(extendDto.MaHopDong) && hd.MaNv == int.Parse(extendDto.MaNv));

                if (contract == null)
                {
                    return NotFound(new { message = "Hợp đồng không tồn tại." });
                }

                // Kiểm tra hợp đồng còn hiệu lực
                if (!contract.IsActive)
                {
                    return BadRequest(new { message = "Hợp đồng không còn hiệu lực." });
                }

                // Kiểm tra ngày kết thúc
                if (!contract.NgayKetThuc.HasValue)
                {
                    return BadRequest(new { message = "Hợp đồng không có ngày kết thúc để gia hạn." });
                }

                var currentDate = DateOnly.FromDateTime(DateTime.Today);
                if (contract.NgayKetThuc < currentDate)
                {
                    return BadRequest(new { message = "Hợp đồng đã hết hạn, không thể gia hạn." });
                }

                // Kiểm tra số lần gia hạn
                var soLanGiaHan = contract.SoLanGiaHan ?? 0;
                var giaHanToiDa = contract.LoaiHopDong.GiaHanToiDa;

                if (extendDto.ConvertToUnlimited)
                {
                    // (MaLoaiHopDong = 2)
                    var loaiHopDongKhongThoiHan = _context.LoaiHopDongs
                        .FirstOrDefault(l => l.MaLoaiHopDong == 2);
                    if (loaiHopDongKhongThoiHan == null)
                    {
                        return BadRequest(new { message = "Loại hợp đồng không xác định thời hạn không tồn tại." });
                    }

                    contract.MaLoaiHopDong = 2;
                    contract.ThoiHan = null;
                    contract.NgayKetThuc = null;
                    contract.SoLanGiaHan = soLanGiaHan;
                    contract.GhiChu = (contract.GhiChu ?? "") + $"\nChuyển sang hợp đồng không xác định thời hạn vào {currentDate}.";
                }
                else
                {
                    // Gia hạn hợp đồng xác định thời hạn
                    if (giaHanToiDa.HasValue && soLanGiaHan >= giaHanToiDa.Value)
                    {
                        return BadRequest(new { message = "Hợp đồng đã đạt số lần gia hạn tối đa." });
                    }

                    // Gia hạn thêm 12 tháng
                    contract.ThoiHan = 12;
                    contract.NgayKetThuc = contract.NgayKetThuc.Value.AddMonths(12);
                    contract.SoLanGiaHan = soLanGiaHan + 1;
                    contract.GhiChu = (contract.GhiChu ?? "") + $"\nGia hạn hợp đồng lần {contract.SoLanGiaHan} vào {currentDate}.";
                }

                _context.HopDongLaoDongs.Update(contract);
                _context.SaveChanges();
                transaction.Commit();

                return Ok(new { message = extendDto.ConvertToUnlimited ? "Chuyển sang hợp đồng không xác định thời hạn thành công." : "Gia hạn hợp đồng thành công." });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Lỗi khi gia hạn hợp đồng.");
                return StatusCode(500, new { message = "Lỗi server. Xem log để biết chi tiết.", error = ex.Message });
            }
        }
        #endregion

        #region Thêm nhân viên 
        [HttpPost("CreateEmployee")]
        public IActionResult CreateEmployee([FromForm] CreateEmployeeDTO employeeDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = string.Join(" | ", errors) });
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Kiểm tra số điện thoại đã tồn tại chưa
                if (_context.TaiKhoans.Any(t => t.Username == employeeDto.Sdt))
                {
                    return Conflict(new { message = "Số điện thoại đã được sử dụng." });
                }

                // Kiểm tra phòng ban & chức vụ hợp lệ
                if (!_context.PhongBans.Any(p => p.MaPhongBan == employeeDto.MaPhongBan))
                {
                    return BadRequest(new { message = "Phòng ban không tồn tại." });
                }

                if (!_context.ChucVus.Any(c => c.MaChucVu == employeeDto.MaChucVu))
                {
                    return BadRequest(new { message = "Chức vụ không tồn tại." });
                }

                // Tạo nhân viên mới
                var employee = new NhanVien
                {
                    HoTen = employeeDto.HoTen,
                    NgaySinh = DateOnly.FromDateTime(employeeDto.NgaySinh),
                    GioiTinh = employeeDto.GioiTinh,
                    DiaChi = employeeDto.DiaChi,
                    Sdt = employeeDto.Sdt,
                    Email = employeeDto.Email,
                    TrinhDoHocVan = employeeDto.TrinhDoHocVan,
                    MaPhongBan = employeeDto.MaPhongBan,
                    MaChucVu = employeeDto.MaChucVu,
                    NgayVaoLam = DateOnly.FromDateTime(employeeDto.NgayVaoLam),
                    AvatarUrl = null,
                    SoNguoiPhuThuoc = employeeDto.SoNguoiPhuThuoc
                };

                _context.NhanViens.Add(employee);
                _context.SaveChanges();
                int maNvMoi = employee.MaNv;

                var defaultRoles = new List<string> { "EMPLOYEE" };
                var validRoles = _context.QuyenHans
                    .Where(q => defaultRoles.Contains(q.MaQuyenHan))
                    .Select(q => q.MaQuyenHan)
                    .ToList();

                if (!validRoles.Any())
                {
                    _logger.LogWarning("Không có quyền hạn hợp lệ để gán cho nhân viên mới.");
                    return BadRequest(new { message = "Không có quyền hạn hợp lệ để gán cho nhân viên mới." });
                }

                // Tạo tài khoản
                string username = _usernameGen.GenerateUsername(string.IsNullOrEmpty(employeeDto.HoTen) ? "user" : employeeDto.HoTen, maNvMoi);
                string defaultPassword = "123456";
                string randomkey = PasswordHelper.GenerateRandomKey();
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(defaultPassword + randomkey);

                var taiKhoan = new TaiKhoan
                {
                    Username = username,
                    PasswordHash = hashedPassword,
                    PasswordSalt = randomkey,
                    MaNv = maNvMoi
                };
                _context.TaiKhoans.Add(taiKhoan);
                _context.SaveChanges();

                // Gán quyền hạn cho tài khoản
                foreach (var role in validRoles)
                {
                    _context.TaiKhoanQuyenHans.Add(new TaiKhoanQuyenHan
                    {
                        Username = taiKhoan.Username,
                        MaQuyenHan = role
                    });
                }
                _context.SaveChanges();

                // Xử lý ảnh đại diện
                if (employeeDto.AvatarUrl != null)
                {
                    try
                    {
                        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        var fileName = $"{maNvMoi}_{Path.GetFileName(employeeDto.AvatarUrl.FileName)}";
                        var filePath = Path.Combine(basePath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            employeeDto.AvatarUrl.CopyTo(stream);
                        }

                        employee.AvatarUrl = $"/avatars/{fileName}";
                        _context.NhanViens.Update(employee);
                        _context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi xử lý ảnh đại diện.");
                    }
                }

                // Gửi email thông báo
                try
                {
                    string subject = "Thông tin tài khoản nhân viên";
                    string body = $@"
            <table width='100%' cellpadding='0' cellspacing='0' style='font-family:Segoe UI, sans-serif; background: #f4f4f4; padding: 30px;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 0 10px rgba(0,0,0,0.05);'>
                            <tr>
                                <td style='background-color: #004080; padding: 20px 0; text-align: center;'>
                                    <img src='' alt='Company Logo' height='50' />
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 30px;'>
                                    <h2 style='color: #004080;'>Chào {employeeDto.HoTen},</h2>
                                    <p>Chúc mừng bạn đã trở thành một phần của công ty 🎉.</p>
                                    <p>Dưới đây là thông tin tài khoản để bạn có thể đăng nhập vào hệ thống:</p>

                                    <table cellpadding='8' cellspacing='0' width='100%' style='margin-top: 20px; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;'>
                                        <tr style='background-color: #f0f0f0;'>
                                            <th align='left'>Tài khoản</th>
                                            <th align='left'>Mật khẩu tạm thời</th>
                                        </tr>
                                        <tr>
                                            <td>{taiKhoan.Username}</td>
                                            <td>{defaultPassword}</td>
                                        </tr>
                                    </table>

                                    <p style='margin-top: 20px; color: #888;'>
                                        * Vui lòng đăng nhập vào hệ thống và đổi mật khẩu để đảm bảo an toàn.
                                    </p>

                                    <div style='text-align:center; margin: 30px 0;'>
                                        <a href='' 
                                            style='display:inline-block; background-color:#004080; color:white; padding:12px 20px; text-decoration:none; border-radius:5px; font-weight:bold;'>
                                            Đăng nhập ngay
                                        </a>
                                    </div>

                                    <p style='font-size: 13px; color: #999; text-align: center;'>
                                        Nếu bạn có bất kỳ câu hỏi nào, hãy liên hệ với bộ phận IT để được hỗ trợ.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style='background-color: #eeeeee; padding: 15px; text-align: center; font-size: 12px; color: #777;'>
                                    © 2025 Công ty ABC | <a href='https://yourcompanydomain.com' style='color:#004080;'>Trang chủ</a>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>";

                    _emailService.SendEmail(employeeDto.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email.");
                }

                transaction.Commit();

                // Tạo DTO để trả về
                var responseDto = new
                {
                    MaNv = employee.MaNv,
                    HoTen = employee.HoTen,
                    NgaySinh = employee.NgaySinh,
                    GioiTinh = employee.GioiTinh.HasValue ? (employee.GioiTinh.Value ? "Nam" : "Nữ") : "Không xác định",
                    DiaChi = employee.DiaChi,
                    Sdt = employee.Sdt,
                    Email = employee.Email,
                    TrinhDoHocVan = employee.TrinhDoHocVan,
                    NgayVaoLam = employee.NgayVaoLam,
                    MaPhongBan = employee.MaPhongBan,
                    MaChucVu = employee.MaChucVu,
                    AvatarUrl = employee.AvatarUrl,
                    SoNguoiPhuThuoc = employee.SoNguoiPhuThuoc
                };

                return CreatedAtAction(nameof(GetEmployees), new { id = maNvMoi }, responseDto);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Lỗi khi tạo nhân viên.");
                return StatusCode(500, new { message = "Lỗi server. Xem log để biết chi tiết.", error = ex.Message });
            }
        }
        #endregion
    }
}