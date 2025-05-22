using HR_KD.Data;
using HR_KD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    public class LeaveManagerApiController : Controller
    {
        private readonly HrDbContext _context;
        private readonly PhepNamService _phepNamService;
        private readonly IConfiguration _configuration;

        public LeaveManagerApiController(HrDbContext context, PhepNamService phepNamService, IConfiguration configuration)
        {
            _context = context;
            _phepNamService = phepNamService;
            _configuration = configuration;
        }

        private int? GetMaNvFromClaims()
        {
            var maNvClaim = User.FindFirst("MaNV")?.Value;
            return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
        }

        [HttpGet]
        [Route("LeaveManager/GetAll")]
        public async Task<IActionResult> GetAllLeaveRequests()
        {
            try
            {
                var data = await (
                    from nn in _context.NgayNghis
                    join nv in _context.NhanViens on nn.MaNv equals nv.MaNv
                    join sdp in _context.SoDuPheps
                        on new { nn.MaNv, Nam = nn.NgayNghi1.Year } equals new { sdp.MaNv, sdp.Nam } into sdpJoin
                    from sdp in sdpJoin.DefaultIfEmpty()
                    join tt in _context.TrangThais on nn.MaTrangThai equals tt.MaTrangThai
                    join lnn in _context.LoaiNgayNghis on nn.MaLoaiNgayNghi equals lnn.MaLoaiNgayNghi into lnnJoin
                    from lnn in lnnJoin.DefaultIfEmpty()
                    orderby nn.NgayNghi1 descending
                    select new
                    {
                        nn.MaNgayNghi,
                        nn.MaDon,
                        nv.MaNv,
                        nv.HoTen,
                        NgayNghi = nn.NgayNghi1.ToString("dd/MM/yyyy"),
                        nn.LyDo,
                        TenLoai = lnn != null ? lnn.TenLoai : "Không xác định",
                        nn.MaTrangThai,
                        TrangThai = tt.TenTrangThai,
                        nn.FileDinhKem,
                        SoNgayConLai = sdp != null ? sdp.SoNgayConLai : 0,
                        NgayCapNhat = nn.NgayLamDon.ToString("dd/MM/yyyy"),
                        nn.LyDoTuChoi,
                        nn.LyDoHuy,
                        nn.GhiChu
                    }).ToListAsync();

                var groupedData = data
                    .GroupBy(x => x.MaDon)
                    .Select(g => new
                    {
                        MaDon = g.Key,
                        MaNgayNghi = g.First().MaNgayNghi,
                        MaNv = g.First().MaNv,
                        HoTen = g.First().HoTen,
                        NgayNghiList = g.Select(x => x.NgayNghi).ToList(),
                        LyDo = g.First().LyDo,
                        TenLoai = g.First().TenLoai,
                        MaTrangThai = g.First().MaTrangThai,
                        TrangThai = g.First().TrangThai,
                        FileDinhKem = ProcessAttachmentFiles(g.First().FileDinhKem),
                        SoNgayConLai = g.First().SoNgayConLai,
                        NgayCapNhat = g.First().NgayCapNhat,
                        TongSoNgay = g.Count(),
                        LyDoTuChoi = g.First().LyDoTuChoi,
                        LyDoHuy = g.First().LyDoHuy,
                        GhiChu = g.First().GhiChu
                    }).ToList();

                var processedData = groupedData.Select(item => new
                {
                    item.MaDon,
                    item.MaNgayNghi,
                    item.MaNv,
                    item.HoTen,
                    item.NgayNghiList,
                    LyDo = !string.IsNullOrEmpty(item.LyDo) ? $"{item.TenLoai} - {item.LyDo}" : item.TenLoai,
                    item.MaTrangThai,
                    TrangThai = item.MaTrangThai == "NN5" ? "Đã duyệt (Không hưởng lương)" : item.TrangThai,
                    item.SoNgayConLai,
                    item.NgayCapNhat,
                    item.FileDinhKem,
                    item.TongSoNgay,
                    item.LyDoTuChoi,
                    item.LyDoHuy,
                    item.GhiChu
                }).ToList();

                return Ok(new { success = true, data = processedData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi lấy dữ liệu nghỉ phép.", error = ex.Message });
            }
        }

        private List<object> ProcessAttachmentFiles(string fileNames)
        {
            var result = new List<object>();
            if (string.IsNullOrEmpty(fileNames))
                return result;

            var files = fileNames.Split('-');
            foreach (var fileName in files)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    result.Add(new
                    {
                        FileName = fileName,
                        FilePath = $"/Uploads/{fileName}"
                    });
                }
            }
            return result;
        }

        [HttpPost]
        [Route("LeaveManager/UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            var ngayNghi = await _context.NgayNghis.FindAsync(request.MaNgayNghi);
            if (ngayNghi == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy bản ghi ngày nghỉ." });
            }

            if (request.TrangThai == "Đã duyệt")
            {
                bool coTheDuyet = await _phepNamService.CheckSoNgayConLaiAsync(request.MaNgayNghi);
                if (!coTheDuyet)
                {
                    var soNgayConLai = await _phepNamService.GetSoNgayConLaiAsync(ngayNghi.MaNv, ngayNghi.NgayNghi1.Year);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Không thể duyệt đơn. Nhân viên không đủ số ngày phép còn lại. Số ngày còn lại: {soNgayConLai}"
                    });
                }
            }

            var nguoiDuyet = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == currentMaNv);
            if (nguoiDuyet == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
            }

            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == ngayNghi.MaNv);
            if (nhanVien == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin nhân viên." });
            }

            string newMaTrangThai;
            bool isPaidLeave = true;
            switch (request.TrangThai)
            {
                case "Đã duyệt":
                    var loaiNgayNghi = await _context.LoaiNgayNghis
                        .FirstOrDefaultAsync(lnn => lnn.MaLoaiNgayNghi == ngayNghi.MaLoaiNgayNghi);
                    if (loaiNgayNghi != null)
                    {
                        if (loaiNgayNghi.TinhVaoPhepNam)
                            newMaTrangThai = "NN2";
                        else if (!loaiNgayNghi.HuongLuong)
                        {
                            newMaTrangThai = "NN5";
                            isPaidLeave = false;
                        }
                        else
                            newMaTrangThai = "NN2";
                    }
                    else
                        newMaTrangThai = "NN2";
                    break;
                case "Từ chối":
                    newMaTrangThai = "NN3";
                    break;
                case "Yêu cầu bổ sung":
                    newMaTrangThai = "NN6";
                    break;
                default:
                    return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var allNgayNghis = await _context.NgayNghis
                    .Where(nn => nn.MaDon == ngayNghi.MaDon)
                    .ToListAsync();

                foreach (var nn in allNgayNghis)
                {
                    nn.MaTrangThai = newMaTrangThai;
                    nn.NgayDuyet = DateTime.Now;
                    nn.NguoiDuyetId = currentMaNv;

                    if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        nn.LyDoTuChoi = request.LyDo;
                        nn.GhiChu = $"Từ chối bởi: {nguoiDuyet.HoTen}";
                    }
                    else if (newMaTrangThai == "NN2")
                    {
                        nn.GhiChu = $"Duyệt bởi: {nguoiDuyet.HoTen}";
                    }
                    else if (newMaTrangThai == "NN6" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        nn.LyDoTuChoi = request.LyDo;
                        nn.GhiChu = $"Yêu cầu bổ sung bởi: {nguoiDuyet.HoTen}";
                    }

                    _context.NgayNghis.Update(nn);

                    DateOnly ngayLamViec = nn.NgayNghi1;
                    var chamCong = await _context.ChamCongs
                        .FirstOrDefaultAsync(cc => cc.MaNv == nn.MaNv && cc.NgayLamViec == ngayLamViec);

                    if (newMaTrangThai == "NN2" || newMaTrangThai == "NN5")
                    {
                        if (chamCong == null)
                        {
                            chamCong = new ChamCong
                            {
                                MaNv = nn.MaNv,
                                NgayLamViec = ngayLamViec,
                                GioVao = new TimeOnly(8, 0),
                                GioRa = new TimeOnly(20, 0),
                                TongGio = 8m,
                                TrangThai = "CC5",
                                MaNvDuyet = currentMaNv.Value
                            };
                            _context.ChamCongs.Add(chamCong);
                        }
                        else
                        {
                            chamCong.GioVao = new TimeOnly(8, 0);
                            chamCong.GioRa = new TimeOnly(20, 0);
                            chamCong.TongGio = 8m;
                            chamCong.TrangThai = "CC5";
                            chamCong.MaNvDuyet = currentMaNv.Value;
                            _context.ChamCongs.Update(chamCong);
                        }
                    }
                    else if (newMaTrangThai == "NN3")
                    {
                        if (chamCong == null)
                        {
                            chamCong = new ChamCong
                            {
                                MaNv = nn.MaNv,
                                NgayLamViec = ngayLamViec,
                                GioVao = new TimeOnly(8, 0),
                                GioRa = new TimeOnly(8, 0),
                                TongGio = 0m,
                                TrangThai = "CC6",
                                MaNvDuyet = currentMaNv.Value
                            };
                            _context.ChamCongs.Add(chamCong);
                        }
                        else
                        {
                            chamCong.GioVao = new TimeOnly(8, 0);
                            chamCong.GioRa = new TimeOnly(8, 0);
                            chamCong.TongGio = 0m;
                            chamCong.TrangThai = "CC6";
                            chamCong.MaNvDuyet = currentMaNv.Value;
                            _context.ChamCongs.Update(chamCong);
                        }

                        var gioThieu = await _context.GioThieus
                            .FirstOrDefaultAsync(gt => gt.MaNv == nn.MaNv && gt.NgayThieu == ngayLamViec);

                        if (gioThieu == null)
                        {
                            gioThieu = new GioThieu
                            {
                                MaNv = nn.MaNv,
                                NgayThieu = ngayLamViec,
                                TongGioThieu = 8m,
                                MaNvNavigation = nhanVien
                            };
                            _context.GioThieus.Add(gioThieu);
                        }
                        else
                        {
                            gioThieu.TongGioThieu = 8m;
                            _context.GioThieus.Update(gioThieu);
                        }

                        var firstDayOfMonth = new DateOnly(ngayLamViec.Year, ngayLamViec.Month, 1);
                        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                        var tongGioThieu = await _context.TongGioThieus
                            .FirstOrDefaultAsync(t => t.MaNv == nn.MaNv &&
                                                    t.NgayBatDauThieu == firstDayOfMonth &&
                                                    t.NgayKetThucThieu == lastDayOfMonth);

                        if (tongGioThieu == null)
                        {
                            tongGioThieu = new TongGioThieu
                            {
                                MaNv = nn.MaNv,
                                NgayBatDauThieu = firstDayOfMonth,
                                NgayKetThucThieu = lastDayOfMonth,
                                TongGioConThieu = 8m,
                                TongGioLamBu = 0m,
                                MaNvNavigation = nhanVien
                            };
                            _context.TongGioThieus.Add(tongGioThieu);
                        }
                        else
                        {
                            tongGioThieu.TongGioConThieu += 8m;
                            _context.TongGioThieus.Update(tongGioThieu);
                        }
                    }
                }

                if (newMaTrangThai == "NN2")
                {
                    await _phepNamService.UpdateSoNgayDaSuDungAsync(request.MaNgayNghi);
                }

                // Send email notification
                var leaveDates = allNgayNghis.Select(nn => nn.NgayNghi1).Distinct().ToList();
                var loaiNgayNghi = await _context.LoaiNgayNghis
                    .FirstOrDefaultAsync(lnn => lnn.MaLoaiNgayNghi == ngayNghi.MaLoaiNgayNghi);
                var tenLoai = loaiNgayNghi != null ? loaiNgayNghi.TenLoai : "Không xác định";
                var soNgayConLai = await _phepNamService.GetSoNgayConLaiAsync(ngayNghi.MaNv, ngayNghi.NgayNghi1.Year);
                if (newMaTrangThai == "NN2" || newMaTrangThai == "NN5")
                {
                    SendApprovalEmail(nhanVien.Email, nhanVien.HoTen, leaveDates, newMaTrangThai, tenLoai, ngayNghi.LyDo, allNgayNghis.Count, nguoiDuyet.HoTen, soNgayConLai);
                }
                else if (newMaTrangThai == "NN3")
                {
                    SendRejectionEmail(nhanVien.Email, nhanVien.HoTen, leaveDates, "NN3", request.LyDo, tenLoai, ngayNghi.LyDo, allNgayNghis.Count, nguoiDuyet.HoTen);
                }
                else if (newMaTrangThai == "NN6")
                {
                    SendSupplementRequestEmail(nhanVien.Email, nhanVien.HoTen, leaveDates, request.LyDo, tenLoai, ngayNghi.LyDo, allNgayNghis.Count, nguoiDuyet.HoTen);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var tenTrangThai = await _context.TrangThais
                    .Where(tt => tt.MaTrangThai == newMaTrangThai)
                    .Select(tt => tt.TenTrangThai)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái thành công.",
                    data = new
                    {
                        nguoiDuyet = nguoiDuyet.HoTen,
                        maTrangThai = newMaTrangThai,
                        trangThai = newMaTrangThai == "NN5" ? "Đã duyệt (Không hưởng lương)" : tenTrangThai,
                        ngayDuyet = DateTime.Now,
                        ghiChu = request.LyDo
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật trạng thái: {ex.Message}" });
            }
        }

        [HttpGet]
        [Route("LeaveManager/GetSummary")]
        public async Task<IActionResult> GetLeaveRequestsSummary()
        {
            var today = DateTime.Today;
            var currentMonth = today.Month;
            var currentYear = today.Year;

            var pendingCount = await _context.NgayNghis
                .Where(nn => nn.MaTrangThai == "NN1")
                .Select(nn => nn.MaDon)
                .Distinct()
                .CountAsync();

            var approvedTodayCount = await _context.NgayNghis
                .Where(nn =>
                    (nn.MaTrangThai == "NN2" || nn.MaTrangThai == "NN5") &&
                    nn.NgayLamDon.Date == today)
                .Select(nn => nn.MaDon)
                .Distinct()
                .CountAsync();

            var currentMonthCount = await _context.NgayNghis
                .Where(nn => nn.NgayNghi1.Month == currentMonth && nn.NgayNghi1.Year == currentYear)
                .Select(nn => nn.MaDon)
                .Distinct()
                .CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    pendingCount,
                    approvedTodayCount,
                    currentMonthCount
                }
            });
        }

        [HttpPost]
        [Route("LeaveManager/BatchUpdateStatus")]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdateStatusRequest request)
        {
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            var nguoiDuyet = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == currentMaNv);
            if (nguoiDuyet == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
            }

            if (request.TrangThai == "Đã duyệt")
            {
                var (canApprove, message) = await _phepNamService.CheckBatchSoNgayConLaiAsync(request.MaNgayNghiList);
                if (!canApprove)
                {
                    return BadRequest(new { success = false, message = message });
                }
            }

            string newMaTrangThai;
            switch (request.TrangThai)
            {
                case "Đã duyệt":
                    var loaiNgayNghis = await _context.NgayNghis
                        .Where(nn => request.MaNgayNghiList.Contains(nn.MaNgayNghi))
                        .Select(nn => nn.MaLoaiNgayNghi)
                        .Distinct()
                        .ToListAsync();

                    var loaiNgayNghiInfo = await _context.LoaiNgayNghis
                        .Where(lnn => loaiNgayNghis.Contains(lnn.MaLoaiNgayNghi))
                        .ToDictionaryAsync(lnn => lnn.MaLoaiNgayNghi, lnn => new { lnn.HuongLuong, lnn.TinhVaoPhepNam });

                    if (loaiNgayNghiInfo.Values.Any(info => info.TinhVaoPhepNam))
                        newMaTrangThai = "NN2";
                    else if (loaiNgayNghiInfo.Values.Any(info => !info.HuongLuong))
                        newMaTrangThai = "NN5";
                    else
                        newMaTrangThai = "NN2";
                    break;
                case "Từ chối":
                    newMaTrangThai = "NN3";
                    break;
                case "Yêu cầu bổ sung":
                    newMaTrangThai = "NN6";
                    break;
                default:
                    return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var maDons = await _context.NgayNghis
                    .Where(nn => request.MaNgayNghiList.Contains(nn.MaNgayNghi))
                    .Select(nn => nn.MaDon)
                    .Distinct()
                    .ToListAsync();

                var allNgayNghis = await _context.NgayNghis
                    .Where(nn => maDons.Contains(nn.MaDon))
                    .ToListAsync();

                var emailNotifications = new Dictionary<int, (string Email, string HoTen, List<DateOnly> LeaveDates, string TenLoai, string LyDo, int TongSoNgay, decimal SoNgayConLai)>();

                foreach (var ngayNghi in allNgayNghis)
                {
                    ngayNghi.MaTrangThai = newMaTrangThai;
                    ngayNghi.NgayDuyet = DateTime.Now;
                    ngayNghi.NguoiDuyetId = currentMaNv;

                    if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        ngayNghi.LyDoTuChoi = request.LyDo;
                        ngayNghi.GhiChu = $"Từ chối bởi: {nguoiDuyet.HoTen}";
                    }
                    else if (newMaTrangThai == "NN2" || newMaTrangThai == "NN5")
                    {
                        ngayNghi.GhiChu = $"Duyệt bởi: {nguoiDuyet.HoTen}";
                    }
                    else if (newMaTrangThai == "NN6" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        ngayNghi.LyDoTuChoi = request.LyDo;
                        ngayNghi.GhiChu = $"Yêu cầu bổ sung bởi: {nguoiDuyet.HoTen}";
                    }

                    _context.NgayNghis.Update(ngayNghi);

                    DateOnly ngayLamViec = ngayNghi.NgayNghi1;
                    var chamCong = await _context.ChamCongs
                        .FirstOrDefaultAsync(cc => cc.MaNv == ngayNghi.MaNv && cc.NgayLamViec == ngayLamViec);

                    if (newMaTrangThai == "NN2" || newMaTrangThai == "NN5")
                    {
                        if (chamCong == null)
                        {
                            chamCong = new ChamCong
                            {
                                MaNv = ngayNghi.MaNv,
                                NgayLamViec = ngayLamViec,
                                GioVao = new TimeOnly(8, 0),
                                GioRa = new TimeOnly(20, 0),
                                TongGio = 8m,
                                TrangThai = "CC5",
                                MaNvDuyet = currentMaNv.Value
                            };
                            _context.ChamCongs.Add(chamCong);
                        }
                        else
                        {
                            chamCong.GioVao = new TimeOnly(8, 0);
                            chamCong.GioRa = new TimeOnly(20, 0);
                            chamCong.TongGio = 8m;
                            chamCong.TrangThai = "CC5";
                            chamCong.MaNvDuyet = currentMaNv.Value;
                            _context.ChamCongs.Update(chamCong);
                        }
                    }
                    else if (newMaTrangThai == "NN3")
                    {
                        if (chamCong == null)
                        {
                            chamCong = new ChamCong
                            {
                                MaNv = ngayNghi.MaNv,
                                NgayLamViec = ngayLamViec,
                                GioVao = new TimeOnly(8, 0),
                                GioRa = new TimeOnly(8, 0),
                                TongGio = 0m,
                                TrangThai = "CC6",
                                MaNvDuyet = currentMaNv.Value
                            };
                            _context.ChamCongs.Add(chamCong);
                        }
                        else
                        {
                            chamCong.GioVao = new TimeOnly(8, 0);
                            chamCong.GioRa = new TimeOnly(8, 0);
                            chamCong.TongGio = 0m;
                            chamCong.TrangThai = "CC6";
                            chamCong.MaNvDuyet = currentMaNv.Value;
                            _context.ChamCongs.Update(chamCong);
                        }

                        var gioThieu = await _context.GioThieus
                            .FirstOrDefaultAsync(gt => gt.MaNv == ngayNghi.MaNv && gt.NgayThieu == ngayLamViec);

                        if (gioThieu == null)
                        {
                            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == ngayNghi.MaNv);
                            gioThieu = new GioThieu
                            {
                                MaNv = ngayNghi.MaNv,
                                NgayThieu = ngayLamViec,
                                TongGioThieu = 8m,
                                MaNvNavigation = nhanVien
                            };
                            _context.GioThieus.Add(gioThieu);
                        }
                        else
                        {
                            gioThieu.TongGioThieu = 8m;
                            _context.GioThieus.Update(gioThieu);
                        }

                        var firstDayOfMonth = new DateOnly(ngayLamViec.Year, ngayLamViec.Month, 1);
                        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                        var tongGioThieu = await _context.TongGioThieus
                            .FirstOrDefaultAsync(t => t.MaNv == ngayNghi.MaNv &&
                                                    t.NgayBatDauThieu == firstDayOfMonth &&
                                                    t.NgayKetThucThieu == lastDayOfMonth);

                        if (tongGioThieu == null)
                        {
                            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == ngayNghi.MaNv);
                            tongGioThieu = new TongGioThieu
                            {
                                MaNv = ngayNghi.MaNv,
                                NgayBatDauThieu = firstDayOfMonth,
                                NgayKetThucThieu = lastDayOfMonth,
                                TongGioConThieu = 8m,
                                TongGioLamBu = 0m,
                                MaNvNavigation = nhanVien
                            };
                            _context.TongGioThieus.Add(tongGioThieu);
                        }
                        else
                        {
                            tongGioThieu.TongGioConThieu += 8m;
                            _context.TongGioThieus.Update(tongGioThieu);
                        }
                    }

                    // Collect email notification data
                    if (!emailNotifications.ContainsKey(ngayNghi.MaNv))
                    {
                        var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == ngayNghi.MaNv);
                        var loaiNgayNghi = await _context.LoaiNgayNghis
                            .FirstOrDefaultAsync(lnn => lnn.MaLoaiNgayNghi == ngayNghi.MaLoaiNgayNghi);
                        var tenLoai = loaiNgayNghi != null ? loaiNgayNghi.TenLoai : "Không xác định";
                        var soNgayConLai = await _phepNamService.GetSoNgayConLaiAsync(ngayNghi.MaNv, ngayNghi.NgayNghi1.Year);
                        if (nhanVien != null)
                        {
                            emailNotifications[ngayNghi.MaNv] = (
                                nhanVien.Email,
                                nhanVien.HoTen,
                                new List<DateOnly> { ngayNghi.NgayNghi1 },
                                tenLoai,
                                ngayNghi.LyDo,
                                maDons.Count,
                                soNgayConLai
                            );
                        }
                    }
                    else
                    {
                        emailNotifications[ngayNghi.MaNv].LeaveDates.Add(ngayNghi.NgayNghi1);
                    }
                }

                if (newMaTrangThai == "NN2" || newMaTrangThai == "NN5")
                {
                    await _phepNamService.UpdateSoNgayDaSuDungBatchAsync(request.MaNgayNghiList);
                }

                // Send email notifications
                foreach (var (maNv, (email, hoTen, leaveDates, tenLoai, lyDo, tongSoNgay, soNgayConLai)) in emailNotifications)
                {
                    if (newMaTrangThai == "NN2" || newMaTrangThai == "NN5")
                    {
                        SendApprovalEmail(email, hoTen, leaveDates, newMaTrangThai, tenLoai, lyDo, tongSoNgay, nguoiDuyet.HoTen, soNgayConLai);
                    }
                    else if (newMaTrangThai == "NN3")
                    {
                        SendRejectionEmail(email, hoTen, leaveDates, "NN3", request.LyDo, tenLoai, lyDo, tongSoNgay, nguoiDuyet.HoTen);
                    }
                    else if (newMaTrangThai == "NN6")
                    {
                        SendSupplementRequestEmail(email, hoTen, leaveDates, request.LyDo, tenLoai, lyDo, tongSoNgay, nguoiDuyet.HoTen);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái hàng loạt thành công.",
                    data = new
                    {
                        nguoiDuyet = nguoiDuyet.HoTen,
                        soLuongDon = maDons.Count,
                        trangThai = newMaTrangThai == "NN5" ? "Đã duyệt (Không hưởng lương)" : request.TrangThai
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật trạng thái hàng loạt: {ex.Message}" });
            }
        }

        private void SendApprovalEmail(string recipientEmail, string employeeName, List<DateOnly> leaveDates, string trangThai, string leaveType, string leaveReason, int totalLeaveDays, string approverName, decimal remainingLeaveBalance)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["SenderEmail"];
            var senderPassword = emailSettings["SenderPassword"];
            var smtpServer = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["Port"]);
            var systemUrl = emailSettings["SystemUrl"] ?? "https://hr-system.example.com";
            var companyName = emailSettings["CompanyName"] ?? "Công ty TNHH XYZ";
            var companyLogoUrl = emailSettings["CompanyLogoUrl"] ?? "https://via.placeholder.com/150x50?text=Logo";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HR Department", senderEmail));
            message.To.Add(new MailboxAddress(employeeName, recipientEmail));

            string subject = trangThai == "NN5" ? "Thông báo duyệt nghỉ phép (Không hưởng lương)" : "Thông báo duyệt nghỉ phép";
            message.Subject = subject;

            var dateList = string.Join(", ", leaveDates.Select(d => d.ToString("dd/MM/yyyy")));
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                 
                    <div style='padding: 20px; background-color: #ffffff;'>
                        <h2 style='color: #1a3c34; margin: 0 0 20px; text-align: center; font-size: 24px;'>{subject}</h2>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Kính gửi <strong>{employeeName}</strong>,</p>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Phòng Nhân sự {companyName} trân trọng thông báo yêu cầu nghỉ phép của bạn đã được <span style='color: #28a745; font-weight: bold;'>DUYỆT</span> bởi <strong>{approverName}</strong>.</p>
                        <div style='background-color: #f1f8f1; padding: 15px; border-radius: 6px; border-left: 4px solid #28a745; margin-bottom: 20px;'>
                            <h3 style='color: #1a3c34; margin: 0 0 10px; font-size: 18px;'>Chi tiết đơn nghỉ phép</h3>
                            <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333; width: 30%;'>Loại nghỉ phép:</td>
                                    <td style='padding: 8px; color: #333;'>{leaveType}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Lý do nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{leaveReason ?? "Không có ghi chú"}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Ngày nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{dateList}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Tổng số ngày nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{totalLeaveDays} ngày</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Số ngày phép còn lại:</td>
                                    <td style='padding: 8px; color: #333;'>{remainingLeaveBalance} ngày</td>
                                </tr>
                                {(trangThai == "NN5" ? "<tr><td colspan='2' style='padding: 8px; color: #e74c3c; font-style: italic;'>Lưu ý: Đây là nghỉ phép không hưởng lương.</td></tr>" : "")}
                            </table>
                        </div>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Vui lòng kiểm tra chi tiết trên hệ thống nhân sự tại: <a href='{systemUrl}' style='color: #007bff; text-decoration: none; font-weight: bold;'>Hệ thống Nhân sự</a>.</p>
                        <p style='color: #333; font-size: 16px; margin: 0;'>Trân trọng,</p>
                        <p style='color: #333; font-size: 16px; margin: 5px 0 0;'><strong>Phòng Nhân sự - {companyName}</strong></p>
                    </div>
                    <div style='text-align: center; padding: 10px; background-color: #e9ecef; border-top: 1px solid #e0e0e0; font-size: 12px; color: #666;'>
                        <p style='margin: 0;'>Liên hệ: <a href='mailto:{senderEmail}' style='color: #007bff; text-decoration: none;'>{senderEmail}</a> | Đây là email tự động, vui lòng không trả lời.</p>
                    </div>
                </div>";

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
                    client.Authenticate(senderEmail, senderPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send approval email: {ex.Message}");
                }
            }
        }

        private void SendRejectionEmail(string recipientEmail, string employeeName, List<DateOnly> leaveDates, string trangThai, string lyDo, string leaveType, string leaveReason, int totalLeaveDays, string approverName)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["SenderEmail"];
            var senderPassword = emailSettings["SenderPassword"];
            var smtpServer = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["Port"]);
            var systemUrl = emailSettings["SystemUrl"] ?? "https://hr-system.example.com";
            var companyName = emailSettings["CompanyName"] ?? "Công ty TNHH XYZ";
            var companyLogoUrl = emailSettings["CompanyLogoUrl"] ?? "https://via.placeholder.com/150x50?text=Logo";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HR Department", senderEmail));
            message.To.Add(new MailboxAddress(employeeName, recipientEmail));

            string subject = "Thông báo từ chối nghỉ phép";
            message.Subject = subject;

            var dateList = string.Join(", ", leaveDates.Select(d => d.ToString("dd/MM/yyyy")));
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                 
                    <div style='padding: 20px; background-color: #ffffff;'>
                        <h2 style='color: #1a3c34; margin: 0 0 20px; text-align: center; font-size: 24px;'>{subject}</h2>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Kính gửi <strong>{employeeName}</strong>,</p>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Phòng Nhân sự {companyName} trân trọng thông báo yêu cầu nghỉ phép của bạn đã bị <span style='color: #dc3545; font-weight: bold;'>TỪ CHỐI</span> bởi <strong>{approverName}</strong>.</p>
                        <div style='background-color: #f8f1f1; padding: 15px; border-radius: 6px; border-left: 4px solid #dc3545; margin-bottom: 20px;'>
                            <h3 style='color: #1a3c34; margin: 0 0 10px; font-size: 18px;'>Chi tiết đơn nghỉ phép</h3>
                            <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333; width: 30%;'>Loại nghỉ phép:</td>
                                    <td style='padding: 8px; color: #333;'>{leaveType}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Lý do nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{leaveReason ?? "Không có ghi chú"}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Ngày nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{dateList}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Tổng số ngày nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{totalLeaveDays} ngày</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Lý do từ chối:</td>
                                    <td style='padding: 8px; color: #333;'>{lyDo ?? "Không có ghi chú"}</td>
                                </tr>
                            </table>
                        </div>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Vui lòng kiểm tra chi tiết trên hệ thống nhân sự tại: <a href='{systemUrl}' style='color: #007bff; text-decoration: none; font-weight: bold;'>Hệ thống Nhân sự</a>.</p>
                        <p style='color: #333; font-size: 16px; margin: 0;'>Trân trọng,</p>
                        <p style='color: #333; font-size: 16px; margin: 5px 0 0;'><strong>Phòng Nhân sự - {companyName}</strong></p>
                    </div>
                    <div style='text-align: center; padding: 10px; background-color: #e9ecef; border-top: 1px solid #e0e0e0; font-size: 12px; color: #666;'>
                        <p style='margin: 0;'>Liên hệ: <a href='mailto:{senderEmail}' style='color: #007bff; text-decoration: none;'>{senderEmail}</a> | Đây là email tự động, vui lòng không trả lời.</p>
                    </div>
                </div>";

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
                    client.Authenticate(senderEmail, senderPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send rejection email: {ex.Message}");
                }
            }
        }

        private void SendSupplementRequestEmail(string recipientEmail, string employeeName, List<DateOnly> leaveDates, string lyDo, string leaveType, string leaveReason, int totalLeaveDays, string approverName)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var senderEmail = emailSettings["SenderEmail"];
            var senderPassword = emailSettings["SenderPassword"];
            var smtpServer = emailSettings["SmtpServer"];
            var port = int.Parse(emailSettings["Port"]);
            var systemUrl = emailSettings["SystemUrl"] ?? "https://hr-system.example.com";
            var companyName = emailSettings["CompanyName"] ?? "Công ty TNHH XYZ";
            var companyLogoUrl = emailSettings["CompanyLogoUrl"] ?? "https://via.placeholder.com/150x50?text=Logo";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("HR Department", senderEmail));
            message.To.Add(new MailboxAddress(employeeName, recipientEmail));

            string subject = "Yêu cầu bổ sung thông tin nghỉ phép";
            message.Subject = subject;

            var dateList = string.Join(", ", leaveDates.Select(d => d.ToString("dd/MM/yyyy")));
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>

                    <div style='padding: 20px; background-color: #ffffff;'>
                        <h2 style='color: #1a3c34; margin: 0 0 20px; text-align: center; font-size: 24px;'>{subject}</h2>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Kính gửi <strong>{employeeName}</strong>,</p>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Phòng Nhân sự {companyName} trân trọng thông báo yêu cầu nghỉ phép của bạn cần <span style='color: #ffc107; font-weight: bold;'>BỔ SUNG THÔNG TIN</span> theo yêu cầu của <strong>{approverName}</strong>.</p>
                        <div style='background-color: #fff8e1; padding: 15px; border-radius: 6px; border-left: 4px solid #ffc107; margin-bottom: 20px;'>
                            <h3 style='color: #1a3c34; margin: 0 0 10px; font-size: 18px;'>Chi tiết đơn nghỉ phép</h3>
                            <table style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333; width: 30%;'>Loại nghỉ phép:</td>
                                    <td style='padding: 8px; color: #333;'>{leaveType}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Lý do nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{leaveReason ?? "Không có ghi chú"}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Ngày nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{dateList}</td>
                                </tr>
                                <tr style='border-bottom: 1px solid #e0e0e0;'>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Tổng số ngày nghỉ:</td>
                                    <td style='padding: 8px; color: #333;'>{totalLeaveDays} ngày</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; font-weight: bold; color: #333;'>Lý do yêu cầu bổ sung:</td>
                                    <td style='padding: 8px; color: #333;'>{lyDo ?? "Không có ghi chú"}</td>
                                </tr>
                            </table>
                        </div>
                        <p style='color: #333; font-size: 16px; margin: 0 0 15px;'>Vui lòng cung cấp thông tin bổ sung trên hệ thống nhân sự tại: <a href='{systemUrl}' style='color: #007bff; text-decoration: none; font-weight: bold;'>Hệ thống Nhân sự</a>.</p>
                        <p style='color: #333; font-size: 16px; margin: 0;'>Trân trọng,</p>
                        <p style='color: #333; font-size: 16px; margin: 5px 0 0;'><strong>Phòng Nhân sự - {companyName}</strong></p>
                    </div>
                    <div style='text-align: center; padding: 10px; background-color: #e9ecef; border-top: 1px solid #e0e0e0; font-size: 12px; color: #666;'>
                        <p style='margin: 0;'>Liên hệ: <a href='mailto:{senderEmail}' style='color: #007bff; text-decoration: none;'>{senderEmail}</a> | Đây là email tự động, vui lòng không trả lời.</p>
                    </div>
                </div>";

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                try
                {
                    client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
                    client.Authenticate(senderEmail, senderPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send supplement request email: {ex.Message}");
                }
            }
        }

        public class UpdateStatusRequest
        {
            public int MaNgayNghi { get; set; }
            public string TrangThai { get; set; }
            public DateTime NgayCapNhat { get; set; }
            public string? LyDo { get; set; }
        }

        public class BatchUpdateStatusRequest
        {
            public List<int> MaNgayNghiList { get; set; }
            public string TrangThai { get; set; }
            public string? LyDo { get; set; }
        }
    }
}