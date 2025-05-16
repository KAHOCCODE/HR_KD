using HR_KD.Data;
using HR_KD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.ApiControllers
{
    public class LeaveManagerApiController : Controller
    {
        private readonly HrDbContext _context;
        private readonly PhepNamService _phepNamService;

        public LeaveManagerApiController(HrDbContext context, PhepNamService phepNamService)
        {
            _context = context;
            _phepNamService = phepNamService;
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

                // Nhóm dữ liệu theo MaDon
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

                // Xử lý thông tin file đính kèm và kết hợp TenLoai với LyDo
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

        // Hàm xử lý thông tin file đính kèm
        private List<object> ProcessAttachmentFiles(string fileNames)
        {
            var result = new List<object>();

            if (string.IsNullOrEmpty(fileNames))
                return result;

            // Tách các tên file được phân cách bằng dấu gạch ngang
            var files = fileNames.Split('-');

            foreach (var fileName in files)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    result.Add(new
                    {
                        FileName = fileName,
                        FilePath = $"/uploads/{fileName}" // Đường dẫn tương đối đến file
                    });
                }
            }

            return result;
        }

        [HttpPost]
        [Route("LeaveManager/UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            // Lấy mã NV từ claims
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            // Tìm ngày nghỉ theo mã
            var ngayNghi = await _context.NgayNghis.FindAsync(request.MaNgayNghi);
            if (ngayNghi == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy bản ghi ngày nghỉ." });
            }

            // Kiểm tra số ngày phép còn lại nếu là duyệt đơn
            if (request.TrangThai == "Đã duyệt")
            {
                bool coTheDuyet = await _phepNamService.CheckSoNgayConLaiAsync(request.MaNgayNghi);
                if (!coTheDuyet)
                {
                    var soNgayConLai = await _phepNamService.GetSoNgayConLaiAsync(ngayNghi.MaNv, ngayNghi.NgayNghi1.Year);
                    return BadRequest(new { 
                        success = false, 
                        message = $"Không thể duyệt đơn. Nhân viên không đủ số ngày phép còn lại. Số ngày còn lại: {soNgayConLai}" 
                    });
                }
            }

            // Lấy thông tin người duyệt (HoTen) từ bảng nhân viên
            var nguoiDuyet = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == currentMaNv);
            if (nguoiDuyet == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
            }

            // Lấy thông tin nhân viên được duyệt
            var nhanVien = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == ngayNghi.MaNv);
            if (nhanVien == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin nhân viên." });
            }

            // Chuyển đổi trangThai từ frontend sang mã trạng thái trong database
            string newMaTrangThai;
            switch (request.TrangThai)
            {
                case "Đã duyệt":
                    // Kiểm tra loại nghỉ
                    var loaiNgayNghi = await _context.LoaiNgayNghis
                        .FirstOrDefaultAsync(lnn => lnn.MaLoaiNgayNghi == ngayNghi.MaLoaiNgayNghi);

                    if (loaiNgayNghi != null)
                    {
                        if (loaiNgayNghi.TinhVaoPhepNam)
                        {
                            newMaTrangThai = "NN2"; // Tính vào phép năm
                        }
                        else if (!loaiNgayNghi.HuongLuong)
                        {
                            newMaTrangThai = "NN5"; // Không hưởng lương
                        }
                        else
                        {
                            newMaTrangThai = "NN2"; // Hưởng lương nhưng không tính vào phép năm
                        }
                    }
                    else
                    {
                        newMaTrangThai = "NN2"; // Mặc định là hưởng lương nếu không tìm thấy loại nghỉ
                    }
                    break;
                case "Từ chối":
                    newMaTrangThai = "NN3";
                    break;
                default:
                    return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            // Bắt đầu giao dịch để đảm bảo tính toàn vẹn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy tất cả các ngày nghỉ có cùng MaDon
                var allNgayNghis = await _context.NgayNghis
                    .Where(nn => nn.MaDon == ngayNghi.MaDon)
                    .ToListAsync();

                foreach (var nn in allNgayNghis)
                {
                    // Cập nhật trạng thái của ngày nghỉ
                    nn.MaTrangThai = newMaTrangThai;
                    nn.NgayDuyet = DateTime.Now;
                    nn.NguoiDuyetId = currentMaNv;

                    // Lưu lý do từ chối vào cột LyDoTuChoi nếu là từ chối
                    if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        nn.LyDoTuChoi = request.LyDo;
                        nn.GhiChu = $"Từ chối bởi: {nguoiDuyet.HoTen}";
                    }
                    else if (newMaTrangThai == "NN2")
                    {
                        nn.GhiChu = $"Duyệt bởi: {nguoiDuyet.HoTen}";
                    }

                    // Cập nhật đơn nghỉ phép
                    _context.NgayNghis.Update(nn);

                    // Xử lý chấm công dựa trên trạng thái
                    DateOnly ngayLamViec = nn.NgayNghi1;

                    var chamCong = await _context.ChamCongs
                        .FirstOrDefaultAsync(cc => cc.MaNv == nn.MaNv && cc.NgayLamViec == ngayLamViec);

                    if (newMaTrangThai == "NN2") // Đã duyệt
                    {
                        if (chamCong == null)
                        {
                            chamCong = new ChamCong
                            {
                                MaNv = nn.MaNv,
                                NgayLamViec = ngayLamViec,
                                GioVao = new TimeOnly(8, 0), // 8:00 AM
                                GioRa = new TimeOnly(20, 0), // 8:00 PM
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
                    else if (newMaTrangThai == "NN3") // Từ chối
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

                        // Thêm hoặc cập nhật GioThieu
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

                        // Cập nhật TongGioThieu
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

                // Nếu trạng thái là "Đã duyệt", cập nhật SoNgayDaSuDung
                if (newMaTrangThai == "NN2")
                {
                    await _phepNamService.UpdateSoNgayDaSuDungAsync(request.MaNgayNghi);
                }

                // Lưu thay đổi vào database
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật trạng thái: {ex.Message}" });
            }

            // Lấy tên trạng thái từ bảng TrangThais
            var tenTrangThai = await _context.TrangThais
                .Where(tt => tt.MaTrangThai == newMaTrangThai)
                .Select(tt => tt.TenTrangThai)
                .FirstOrDefaultAsync();

            // Trả về thông tin chi tiết bao gồm tên của người duyệt và nhân viên
            return Ok(new
            {
                success = true,
                message = "Cập nhật trạng thái thành công.",
                data = new
                {
                    nguoiDuyet = nguoiDuyet.HoTen,
                    maTrangThai = newMaTrangThai,
                    trangThai = tenTrangThai,
                    ngayDuyet = DateTime.Now,
                    ghiChu = request.LyDo
                }
            });
        }

        [HttpGet]
        [Route("LeaveManager/GetSummary")]
        public async Task<IActionResult> GetLeaveRequestsSummary()
        {
            // Lấy ngày hiện tại
            var today = DateTime.Today;
            // Lấy thông tin tháng hiện tại
            var currentMonth = today.Month;
            var currentYear = today.Year;

            // Đếm số đơn chờ duyệt (theo MaDon duy nhất)
            var pendingCount = await _context.NgayNghis
                .Where(nn => nn.MaTrangThai == "NN1")
                .Select(nn => nn.MaDon)
                .Distinct()
                .CountAsync();

            // Đếm số đơn đã duyệt trong ngày hiện tại (theo MaDon duy nhất)
            var approvedTodayCount = await _context.NgayNghis
                .Where(nn => nn.MaTrangThai == "NN2" && nn.NgayLamDon.Date == today)
                .Select(nn => nn.MaDon)
                .Distinct()
                .CountAsync();

            // Đếm tổng số đơn trong tháng hiện tại (theo MaDon duy nhất)
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
                    pendingCount,           // Số đơn chờ duyệt
                    approvedTodayCount,     // Số đơn đã duyệt hôm nay
                    currentMonthCount       // Tổng số đơn trong tháng hiện tại
                }
            });
        }

        [HttpPost]
        [Route("LeaveManager/BatchUpdateStatus")]
        public async Task<IActionResult> BatchUpdateStatus([FromBody] BatchUpdateStatusRequest request)
        {
            // Lấy mã NV từ claims
            var currentMaNv = GetMaNvFromClaims();
            if (currentMaNv == null)
            {
                return Unauthorized(new { success = false, message = "Chưa xác thực người dùng." });
            }

            // Lấy thông tin người duyệt
            var nguoiDuyet = await _context.NhanViens.FirstOrDefaultAsync(nv => nv.MaNv == currentMaNv);
            if (nguoiDuyet == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
            }

            // Kiểm tra số ngày phép còn lại nếu là duyệt đơn
            if (request.TrangThai == "Đã duyệt")
            {
                var (canApprove, message) = await _phepNamService.CheckBatchSoNgayConLaiAsync(request.MaNgayNghiList);
                if (!canApprove)
                {
                    return BadRequest(new { success = false, message = message });
                }
            }

            // Chuyển đổi trangThai từ frontend sang mã trạng thái trong database
            string newMaTrangThai;
            switch (request.TrangThai)
            {
                case "Đã duyệt":
                    // Lấy tất cả các loại ngày nghỉ liên quan
                    var loaiNgayNghis = await _context.NgayNghis
                        .Where(nn => request.MaNgayNghiList.Contains(nn.MaNgayNghi))
                        .Select(nn => nn.MaLoaiNgayNghi)
                        .Distinct()
                        .ToListAsync();

                    var loaiNgayNghiInfo = await _context.LoaiNgayNghis
                        .Where(lnn => loaiNgayNghis.Contains(lnn.MaLoaiNgayNghi))
                        .ToDictionaryAsync(lnn => lnn.MaLoaiNgayNghi, lnn => new { lnn.HuongLuong, lnn.TinhVaoPhepNam });

                    // Kiểm tra theo thứ tự ưu tiên
                    if (loaiNgayNghiInfo.Values.Any(info => info.TinhVaoPhepNam))
                    {
                        newMaTrangThai = "NN2"; // Tính vào phép năm
                    }
                    else if (loaiNgayNghiInfo.Values.Any(info => !info.HuongLuong))
                    {
                        newMaTrangThai = "NN5"; // Không hưởng lương
                    }
                    else
                    {
                        newMaTrangThai = "NN2"; // Hưởng lương nhưng không tính vào phép năm
                    }
                    break;
                case "Từ chối":
                    newMaTrangThai = "NN3";
                    break;
                default:
                    return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });
            }

            // Bắt đầu giao dịch
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy tất cả các MaDon từ danh sách MaNgayNghi
                var maDons = await _context.NgayNghis
                    .Where(nn => request.MaNgayNghiList.Contains(nn.MaNgayNghi))
                    .Select(nn => nn.MaDon)
                    .Distinct()
                    .ToListAsync();

                // Lấy tất cả các ngày nghỉ có MaDon trong danh sách
                var allNgayNghis = await _context.NgayNghis
                    .Where(nn => maDons.Contains(nn.MaDon))
                    .ToListAsync();

                foreach (var ngayNghi in allNgayNghis)
                {
                    // Cập nhật trạng thái của ngày nghỉ
                    ngayNghi.MaTrangThai = newMaTrangThai;
                    ngayNghi.NgayDuyet = DateTime.Now;
                    ngayNghi.NguoiDuyetId = currentMaNv;

                    // Lưu lý do từ chối nếu có
                    if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        ngayNghi.LyDoTuChoi = request.LyDo;
                        ngayNghi.GhiChu = $"Từ chối bởi: {nguoiDuyet.HoTen}";
                    }
                    else if (newMaTrangThai == "NN2")
                    {
                        ngayNghi.GhiChu = $"Duyệt bởi: {nguoiDuyet.HoTen}";
                    }

                    // Cập nhật đơn nghỉ phép
                    _context.NgayNghis.Update(ngayNghi);

                    // Xử lý chấm công
                    DateOnly ngayLamViec = ngayNghi.NgayNghi1;
                    var chamCong = await _context.ChamCongs
                        .FirstOrDefaultAsync(cc => cc.MaNv == ngayNghi.MaNv && cc.NgayLamViec == ngayLamViec);

                    if (newMaTrangThai == "NN2") // Đã duyệt
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
                    else if (newMaTrangThai == "NN3") // Từ chối
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
                    }
                }

                // Nếu trạng thái là "Đã duyệt", cập nhật SoNgayDaSuDung cho tất cả các đơn
                if (newMaTrangThai == "NN2")
                {
                    await _phepNamService.UpdateSoNgayDaSuDungBatchAsync(request.MaNgayNghiList);
                }

                // Lưu thay đổi vào database
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
                        trangThai = request.TrangThai
                    }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật trạng thái hàng loạt: {ex.Message}" });
            }
        }

        public class UpdateStatusRequest
        {
            public int MaNgayNghi { get; set; }
            public string TrangThai { get; set; } // Changed from MaTrangThai
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