﻿using HR_KD.Data;
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
        [HttpGet]
        [Route("LeaveManager/GetAll")]
        public async Task<IActionResult> GetAllLeaveRequests()
        {
            var data = await (
                from nn in _context.NgayNghis
                join nv in _context.NhanViens on nn.MaNv equals nv.MaNv
                join sdp in _context.SoDuPheps
                    on new { nn.MaNv, Nam = nn.NgayNghi1.Year } equals new { sdp.MaNv, sdp.Nam } into sdpJoin
                from sdp in sdpJoin.DefaultIfEmpty()
                orderby nn.NgayNghi1 descending
                select new
                {
                    nn.MaNgayNghi,
                    nv.MaNv,
                    nv.HoTen,
                    NgayNghi = nn.NgayNghi1.ToString("dd/MM/yyyy"),
                    nn.LyDo,
                    nn.TrangThai,
                    SoNgayConLai = sdp != null ? sdp.SoNgayConLai : 0,
                    NgayCapNhat = nn.NgayCapNhat.ToString("dd/MM/yyyy") 
                }).ToListAsync();

            return Ok(new { success = true, data });
        }

        [HttpPost]
        [Route("LeaveManager/UpdateStatus")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            var ngayNghi = await _context.NgayNghis.FindAsync(request.MaNgayNghi);
            if (ngayNghi == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy bản ghi." });
            }

            // Cập nhật trạng thái và ngày cập nhật
            ngayNghi.TrangThai = request.TrangThai;
            ngayNghi.NgayCapNhat = DateTime.Now; // Thêm dòng này

            _context.NgayNghis.Update(ngayNghi);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công." });
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
                .CountAsync(nn => nn.TrangThai == "Chờ duyệt");

            // Đếm số đơn đã duyệt trong ngày hiện tại
            var approvedTodayCount = await _context.NgayNghis
      .CountAsync(nn => nn.TrangThai == "Đã duyệt" &&
                        nn.NgayCapNhat.Date == today);

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
            public string TrangThai { get; set; }
            public DateTime NgayCapNhat { get; set; }
        }



    }
}
