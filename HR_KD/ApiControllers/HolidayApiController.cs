using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace HR_KD.ApiControllers
{
    [Route("api/Holidays")]
    [ApiController]
    public class HolidayApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public HolidayApiController(HrDbContext context)
        {
            _context = context;
        }

        [HttpPost("Add")]
        public IActionResult AddHoliday(HolidayDTO holidayDto)
        {
            if (holidayDto == null || string.IsNullOrEmpty(holidayDto.TenNgayLe) || holidayDto.NgayLe1 == default)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            // Kiểm tra xem ngày lễ đã tồn tại chưa
            var existingHoliday = _context.NgayLes
                .FirstOrDefault(h => h.NgayLe1 == DateOnly.FromDateTime(holidayDto.NgayLe1));

            if (existingHoliday != null)
            {
                return BadRequest(new { success = false, message = "Ngày lễ này đã tồn tại trong hệ thống." });
            }

            // Thêm ngày lễ vào bảng NgayLe
            var holiday = new NgayLe
            {
                TenNgayLe = holidayDto.TenNgayLe,
                NgayLe1 = DateOnly.FromDateTime(holidayDto.NgayLe1), // Chuyển đổi từ DateTime sang DateOnly
                SoNgayNghi = holidayDto.SoNgayNghi,
                MoTa = holidayDto.MoTa
            };

            _context.NgayLes.Add(holiday);
            _context.SaveChanges();

            // Lấy danh sách tất cả nhân viên để cập nhật chấm công
            var allEmployees = _context.NhanViens.ToList();

            foreach (var employee in allEmployees)
            {
                var attendance = new ChamCong
                {
                    MaNv = employee.MaNv,
                    NgayLamViec = holiday.NgayLe1,
                    GioVao = new TimeOnly(8, 0, 0), // 08:00 AM
                    GioRa = new TimeOnly(17, 0, 0), // 17:00 PM
                    TongGio = 8.0m, // 8 tiếng làm việc
                    TrangThai = "Đã duyệt",
                    GhiChu = $"Ngày lễ: {holiday.TenNgayLe}"
                };

                _context.ChamCongs.Add(attendance);
            }

            _context.SaveChanges();

            return Ok(new { success = true, message = "Ngày lễ đã được thêm và chấm công cho nhân viên!" });
        }

        [HttpGet("GetAll")]
        public IActionResult GetAllHolidays()
        {
            var holidays = _context.NgayLes.ToList();
            return Ok(holidays);
        }

        [HttpDelete("Delete/{id}")]
        public IActionResult DeleteHoliday(int id)
        {
            try
            {
                // Tìm ngày lễ theo ID
                var holiday = _context.NgayLes.Find(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                // Lưu lại ngày lễ để xóa chấm công
                var holidayDate = holiday.NgayLe1;

                // Tìm tất cả các bản ghi chấm công có ngày trùng với ngày lễ
                var attendanceRecords = _context.ChamCongs
                    .Where(c => c.NgayLamViec == holidayDate)
                    .ToList();

                // Đếm số lượng bản ghi chấm công sẽ bị xóa
                int attendanceCount = attendanceRecords.Count;

                // Xóa tất cả các bản ghi chấm công liên quan
                if (attendanceRecords.Any())
                {
                    _context.ChamCongs.RemoveRange(attendanceRecords);
                }

                // Xóa ngày lễ
                _context.NgayLes.Remove(holiday);

                // Lưu các thay đổi
                _context.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Đã xóa ngày lễ và tất cả chấm công liên quan.",
                    deletedAttendanceCount = attendanceCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xóa ngày lễ.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("Details/{id}")]
        public IActionResult GetHolidayDetails(int id)
        {
            var holiday = _context.NgayLes.Find(id);
            if (holiday == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
            }

            return Ok(holiday);
        }

        [HttpGet("CheckExisting")]
        public IActionResult CheckExistingHoliday(DateTime date)
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var holiday = _context.NgayLes.FirstOrDefault(h => h.NgayLe1 == dateOnly);

            return Ok(new
            {
                exists = holiday != null,
                holiday = holiday
            });
        }

        [HttpGet("GetByYear/{year}")]
        public IActionResult GetHolidaysByYear(int year)
        {
            var holidays = _context.NgayLes
                .Where(h => h.NgayLe1.Year == year)
                .ToList();

            return Ok(holidays);
        }
    }
}
