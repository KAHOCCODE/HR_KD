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
                    orderby nn.NgayNghi1 descending
                    select new
                    {
                        nn.MaNgayNghi,
                        nv.MaNv,
                        nv.HoTen,
                        NgayNghi = nn.NgayNghi1.ToString("dd/MM/yyyy"),
                        nn.LyDo,
                        nn.MaTrangThai,
                        TrangThai = tt.TenTrangThai, // Renamed for frontend consistency
                        nn.FileDinhKem,
                        SoNgayConLai = sdp != null ? sdp.SoNgayConLai : 0,
                        NgayCapNhat = nn.NgayLamDon.ToString("dd/MM/yyyy")
                    }).ToListAsync();

                // Xử lý thông tin file đính kèm
                var processedData = data.Select(item => new
                {
                    item.MaNgayNghi,
                    item.MaNv,
                    item.HoTen,
                    item.NgayNghi,
                    item.LyDo,
                    item.MaTrangThai,
                    item.TrangThai, // Using the properly renamed field
                    item.SoNgayConLai,
                    item.NgayCapNhat,
                    FileDinhKem = ProcessAttachmentFiles(item.FileDinhKem)
                }).ToList();

                return Ok(new { success = true, data = processedData });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
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
                    newMaTrangThai = "NN2";
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
                // Cập nhật trạng thái của ngày nghỉ
                ngayNghi.MaTrangThai = newMaTrangThai;
                ngayNghi.NgayDuyet = DateTime.Now;
                ngayNghi.NguoiDuyetId = currentMaNv;

                // Lưu lý do từ chối vào cột GhiChu nếu là từ chối
                if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
                {
                    ngayNghi.GhiChu = request.LyDo;
                }

                // Cập nhật đơn nghỉ phép
                _context.NgayNghis.Update(ngayNghi);

                // Xử lý chấm công dựa trên trạng thái
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

                    // Thêm hoặc cập nhật GioThieu
                    var gioThieu = await _context.GioThieus
                        .FirstOrDefaultAsync(gt => gt.MaNv == ngayNghi.MaNv && gt.NgayThieu == ngayLamViec);

                    if (gioThieu == null)
                    {
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

                    // Cập nhật TongGioThieu
                    var firstDayOfMonth = new DateOnly(ngayLamViec.Year, ngayLamViec.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    var tongGioThieu = await _context.TongGioThieus
                        .FirstOrDefaultAsync(t => t.MaNv == ngayNghi.MaNv &&
                                                t.NgayBatDauThieu == firstDayOfMonth &&
                                                t.NgayKetThucThieu == lastDayOfMonth);

                    if (tongGioThieu == null)
                    {
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
                    maTrangThai = ngayNghi.MaTrangThai,
                    trangThai = tenTrangThai,
                    ngayDuyet = ngayNghi.NgayDuyet,
                    ghiChu = ngayNghi.GhiChu
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

            // Đếm số đơn chờ duyệt
            var pendingCount = await _context.NgayNghis
                .CountAsync(nn => nn.MaTrangThai == "NN1");

            // Đếm số đơn đã duyệt trong ngày hiện tại
            var approvedTodayCount = await _context.NgayNghis
                .CountAsync(nn => nn.MaTrangThai == "NN2" &&
                                 nn.NgayLamDon.Date == today);

            // Đếm tổng số đơn trong tháng hiện tại
            var currentMonthCount = await _context.NgayNghis
                .CountAsync(nn => nn.NgayNghi1.Month == currentMonth &&
                                 nn.NgayNghi1.Year == currentYear);

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

            // Chuyển đổi trangThai từ frontend sang mã trạng thái trong database
            string newMaTrangThai;
            switch (request.TrangThai)
            {
                case "Đã duyệt":
                    newMaTrangThai = "NN2";
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
                foreach (var maNgayNghi in request.MaNgayNghiList)
                {
                    var ngayNghi = await _context.NgayNghis.FindAsync(maNgayNghi);
                    if (ngayNghi == null) continue;

                    // Cập nhật trạng thái của ngày nghỉ
                    ngayNghi.MaTrangThai = newMaTrangThai;
                    ngayNghi.NgayDuyet = DateTime.Now;
                    ngayNghi.NguoiDuyetId = currentMaNv;

                    // Lưu lý do từ chối nếu có
                    if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
                    {
                        ngayNghi.GhiChu = request.LyDo;
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
                        soLuongDon = request.MaNgayNghiList.Count,
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