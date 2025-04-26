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

            var existingHoliday = _context.NgayLes
                .FirstOrDefault(h => h.NgayLe1 == DateOnly.FromDateTime(holidayDto.NgayLe1));

            if (existingHoliday != null)
            {
                return BadRequest(new { success = false, message = "Ngày lễ này đã tồn tại trong hệ thống." });
            }

            var holiday = new NgayLe
            {
                TenNgayLe = holidayDto.TenNgayLe,
                NgayLe1 = DateOnly.FromDateTime(holidayDto.NgayLe1),
                SoNgayNghi = holidayDto.SoNgayNghi,
                MoTa = holidayDto.MoTa,
                TrangThai = "Chờ duyệt"
            };

            _context.NgayLes.Add(holiday);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Ngày lễ đã được thêm !" });
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
                var holiday = _context.NgayLes.Find(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                var holidayDate = holiday.NgayLe1;

                var attendanceRecords = _context.ChamCongs
                    .Where(c => c.NgayLamViec == holidayDate)
                    .ToList();

                int attendanceCount = attendanceRecords.Count;

                if (attendanceRecords.Any())
                {
                    _context.ChamCongs.RemoveRange(attendanceRecords);
                }

                _context.NgayLes.Remove(holiday);

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
        public IActionResult GetHolidaysByYear(int? year) // Change int to int?
        {
            IQueryable<NgayLe> holidays = _context.NgayLes;

            if (year.HasValue)
            {
                holidays = holidays.Where(h => h.NgayLe1.Year == year.Value);
            }

            return Ok(holidays.ToList());
        }

        [HttpPost("Cancel/{id}")] // Hoặc [HttpPatch]
        public IActionResult CancelHoliday(int id)
        {
            try
            {
                var holiday = _context.NgayLes.Find(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                var holidayDate = holiday.NgayLe1;

                var attendanceRecords = _context.ChamCongs
                    .Where(c => c.NgayLamViec == holidayDate)
                    .ToList();

                if (attendanceRecords.Any())
                {
                    _context.ChamCongs.RemoveRange(attendanceRecords);
                }

                holiday.TrangThai = "Chờ duyệt";
                _context.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Đã hủy ngày lễ, trạng thái chuyển về 'Chờ duyệt' và tất cả chấm công liên quan đã bị xóa."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi hủy ngày lễ.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("Approve/{id}")]
        public IActionResult ApproveHoliday(int id)
        {
            try
            {
                var holiday = _context.NgayLes.Find(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                holiday.TrangThai = "Đã duyệt";
                _context.SaveChanges();

                // Gửi thông báo (ví dụ: email, push notification) ở đây nếu cần

                return Ok(new { success = true, message = "Ngày lễ đã được duyệt." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi duyệt ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("Reject/{id}")]
        public IActionResult RejectHoliday(int id)
        {
            try
            {
                var holiday = _context.NgayLes.Find(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                holiday.TrangThai = "Đã từ chối";
                _context.SaveChanges();

                return Ok(new { success = true, message = "Ngày lễ đã bị từ chối." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi từ chối ngày lễ.", error = ex.Message });
            }
        }

        [HttpGet("years")]
        public IActionResult GetHolidayYears()
        {
            var years = _context.NgayLes.Select(h => h.NgayLe1.Year).Distinct().OrderByDescending(y => y).ToList();
            return Ok(years);
        }

        [HttpPost("Approve/year/{year}")]
        public IActionResult ApproveAllHolidaysInYear(int year)
        {
            try
            {
                var holidaysToApprove = _context.NgayLes
                    .Where(h => h.NgayLe1.Year == year && h.TrangThai == "Chờ duyệt")
                    .ToList();

                if (holidaysToApprove.Count == 0)
                {
                    return NotFound("Không có ngày lễ nào để duyệt trong năm này.");
                }

                foreach (var holiday in holidaysToApprove)
                {
                    holiday.TrangThai = "Đã duyệt";
                }

                _context.SaveChanges();

                return Ok($"Đã duyệt {holidaysToApprove.Count} ngày lễ trong năm {year}.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi duyệt ngày lễ.", error = ex.Message });
            }
        }
    }
}