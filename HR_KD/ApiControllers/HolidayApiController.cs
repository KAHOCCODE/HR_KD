using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.Controllers
{
    [ApiController]
    [Route("api/Holidays")]
    [Authorize]
    public class HolidayApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public HolidayApiController(HrDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var holidays = await _context.NgayLes.ToListAsync();
                return Ok(holidays);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tải danh sách ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("Add")]
        public async Task<ActionResult> Add([FromBody] NgayLe holiday)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(holiday.TenNgayLe) || holiday.NgayLe1 == default)
                {
                    return BadRequest(new { success = false, message = "Tên ngày lễ và ngày lễ là bắt buộc." });
                }

                // Check if holiday management is allowed
                var currentDate = DateTime.Now;
                var currentYear = currentDate.Year;
                var isInPeriod = currentDate.Month == 12 && currentDate.Day >= 1 && currentDate.Day <= 30;
                var hasApprovedRequest = await _context.YeuCaus
                    .AnyAsync(y => y.TenYeuCau == "Yêu cầu mở duyệt ngày lễ" &&
                                   y.MoTa.Contains($"năm {currentYear}") &&
                                   y.TrangThai &&
                                   y.NgayDuyet.HasValue &&
                                   y.NgayDuyet.Value >= DateTime.Now.AddDays(-30));

                if (!isInPeriod && !hasApprovedRequest)
                {
                    return BadRequest(new { success = false, message = "Chức năng thêm ngày lễ chỉ được mở trong khoảng thời gian từ 1/12 đến 30/12 hoặc khi có yêu cầu được duyệt cho năm hiện tại." });
                }

                // Check for duplicate holiday
                var existingHoliday = await _context.NgayLes
                    .FirstOrDefaultAsync(h => h.NgayLe1 == holiday.NgayLe1);
                if (existingHoliday != null)
                {
                    return BadRequest(new { success = false, message = $"Ngày lễ '{existingHoliday.TenNgayLe}' đã tồn tại cho ngày {holiday.NgayLe1}." });
                }

                // Set TrangThai based on day of week
                var dayOfWeek = holiday.NgayLe1.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                holiday.TrangThai = holiday.TenNgayLe.Contains("Nghỉ bù") ? "NL3" :
                                   (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday) ? "NL2" : "NL1";

                _context.NgayLes.Add(holiday);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Thêm ngày lễ thành công.", holiday });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi thêm ngày lễ.", error = ex.Message });
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var holiday = await _context.NgayLes.FindAsync(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                // Check if holiday management is allowed
                var currentDate = DateTime.Now;
                var currentYear = currentDate.Year;
                var isInPeriod = currentDate.Month == 12 && currentDate.Day >= 1 && currentDate.Day <= 30;
                var hasApprovedRequest = await _context.YeuCaus
                    .AnyAsync(y => y.TenYeuCau == "Yêu cầu mở duyệt ngày lễ" &&
                                   y.MoTa.Contains($"năm {currentYear}") &&
                                   y.TrangThai &&
                                   y.NgayDuyet.HasValue &&
                                   y.NgayDuyet.Value >= DateTime.Now.AddDays(-30));

                if (!isInPeriod && !hasApprovedRequest)
                {
                    return BadRequest(new { success = false, message = "Chức năng xóa ngày lễ chỉ được mở trong khoảng thời gian từ 1/12 đến 30/12 hoặc khi có yêu cầu được duyệt cho năm hiện tại." });
                }

                // Delete related attendance records
                var deletedAttendanceCount = await _context.ChamCongs
                    .Where(cc => cc.NgayLamViec == holiday.NgayLe1)
                    .ExecuteDeleteAsync();

                _context.NgayLes.Remove(holiday);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Xóa ngày lễ thành công.", deletedAttendanceCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi xóa ngày lễ.", error = ex.Message });
            }
        }

        [HttpGet("CheckOpenApprovalRequest")]
        public async Task<ActionResult> CheckOpenApprovalRequest(int year)
        {
            try
            {
                var exists = await _context.YeuCaus
                    .AnyAsync(y => y.TenYeuCau == "Yêu cầu mở duyệt ngày lễ" &&
                                   y.MoTa.Contains($"năm {year}") &&
                                   !y.TrangThai);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi kiểm tra yêu cầu.", error = ex.Message });
            }
        }

        [HttpGet("CheckApprovedRequest")]
        public async Task<ActionResult> CheckApprovedRequest(int year)
        {
            try
            {
                var exists = await _context.YeuCaus
                    .AnyAsync(y => y.TenYeuCau == "Yêu cầu mở duyệt ngày lễ" &&
                                   y.MoTa.Contains($"năm {year}") &&
                                   y.TrangThai &&
                                   y.NgayDuyet.HasValue &&
                                   y.NgayDuyet.Value >= DateTime.Now.AddDays(-30));
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi kiểm tra yêu cầu đã duyệt.", error = ex.Message });
            }
        }

        [HttpPost("RequestOpenApproval")]
        public async Task<ActionResult> RequestOpenApproval([FromBody] YeuCau request)
        {
            try
            {
                // Validate input
                if (request.TenYeuCau != "Yêu cầu mở duyệt ngày lễ" || string.IsNullOrEmpty(request.MoTa))
                {
                    return BadRequest(new { success = false, message = "Yêu cầu không hợp lệ." });
                }

                // Check if a pending request already exists
                var yearMatch = System.Text.RegularExpressions.Regex.Match(request.MoTa, @"năm (\d{4})");
                if (!yearMatch.Success)
                {
                    return BadRequest(new { success = false, message = "Mô tả yêu cầu phải chứa năm cụ thể (e.g., năm 2025)." });
                }
                var year = int.Parse(yearMatch.Groups[1].Value);

                var existingRequest = await _context.YeuCaus
                    .AnyAsync(y => y.TenYeuCau == "Yêu cầu mở duyệt ngày lễ" &&
                                   y.MoTa.Contains($"năm {year}") &&
                                   !y.TrangThai);
                if (existingRequest)
                {
                    return BadRequest(new { success = false, message = $"Đã có yêu cầu mở duyệt đang chờ xử lý cho năm {year}." });
                }

                // Add request
                request.NgayTao = DateTime.Now;
                request.MaNvTao = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                request.TrangThai = false;

                _context.YeuCaus.Add(request);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Yêu cầu đã được gửi thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi gửi yêu cầu.", error = ex.Message });
            }
        }
    }
}