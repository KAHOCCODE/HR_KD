using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.ApiControllers
{
    public class LeaveManagerApiController : Controller
    {
        private readonly HrDbContext _context;

        public LeaveManagerApiController(HrDbContext context)
        {
            _context = context;
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

            // Kiểm tra trạng thái cũ để tránh trừ phép nhiều lần nếu cập nhật trùng
            bool isAlreadyApproved = ngayNghi.MaTrangThai == "NN2";
            if (newMaTrangThai == "NN2" && !isAlreadyApproved)
            {
                var nam = ngayNghi.NgayNghi1.Year;
                var sdp = await _context.SoDuPheps
                    .FirstOrDefaultAsync(x => x.MaNv == ngayNghi.MaNv && x.Nam == nam);
                if (sdp == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy số dư phép của nhân viên." });
                }
                if (sdp.SoNgayConLai <= 0)
                {
                    return BadRequest(new { success = false, message = "Nhân viên không còn ngày nghỉ phép." });
                }
                // Trừ 1 ngày phép
                sdp.SoNgayConLai -= 1;
                sdp.NgayCapNhat = DateTime.Now;
                _context.SoDuPheps.Update(sdp);
            }

            // Cập nhật trạng thái của ngày nghỉ
            ngayNghi.MaTrangThai = newMaTrangThai;
            ngayNghi.NgayDuyet = DateTime.Now;
            ngayNghi.NguoiDuyetId = currentMaNv;  // Lưu ID người duyệt

            // Lưu lý do từ chối vào cột GhiChu nếu là từ chối
            if (newMaTrangThai == "NN3" && !string.IsNullOrEmpty(request.LyDo))
            {
                ngayNghi.GhiChu = request.LyDo;
            }

            _context.NgayNghis.Update(ngayNghi);
            await _context.SaveChangesAsync();

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

        public class UpdateStatusRequest
        {
            public int MaNgayNghi { get; set; }
            public string TrangThai { get; set; } // Changed from MaTrangThai
            public DateTime NgayCapNhat { get; set; }
            public string? LyDo { get; set; }
        }
    }
}