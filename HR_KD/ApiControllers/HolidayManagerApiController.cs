using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace HR_KD.Controllers
{
    [ApiController]
    [Route("api/holidaymanager")]
    [Authorize]
    public class HolidayManagerApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly IConfiguration _configuration;

        public HolidayManagerApiController(HrDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NgayLe>>> GetHolidays(int? year)
        {
            try
            {
                IQueryable<NgayLe> holidays = _context.NgayLes;
                if (year.HasValue)
                {
                    holidays = holidays.Where(h => h.NgayLe1.Year == year.Value);
                }
                var result = await holidays.ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tải danh sách ngày lễ.", error = ex.Message });
            }
        }

        [HttpGet("years")]
        public async Task<ActionResult<IEnumerable<int>>> GetHolidayYears()
        {
            try
            {
                var years = await _context.NgayLes
                    .Select(h => h.NgayLe1.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync();
                return Ok(years);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tải danh sách năm.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NgayLe>> GetHoliday(int id)
        {
            try
            {
                var holiday = await _context.NgayLes.FindAsync(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }
                return Ok(holiday);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tải thông tin ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("approve/{id}")]
        
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var approverMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                var approverName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

                if (string.IsNullOrEmpty(approverMaNV))
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
                }

                var holiday = await _context.NgayLes.FindAsync(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                if (holiday.TrangThai == TrangThai.NL4 || holiday.TrangThai == TrangThai.NL5)
                {
                    return Ok(new { success = true, message = $"Ngày lễ ID {id} đã được duyệt trước đó." });
                }

                if (holiday.TrangThai == TrangThai.NL1 || holiday.TrangThai == TrangThai.NL2)
                {
                    if (holiday.TrangThai == TrangThai.NL1)
                    {
                        holiday.TrangThai = TrangThai.NL4;
                        var allEmployees = await _context.NhanViens.ToListAsync();
                        foreach (var employee in allEmployees)
                        {
                            var existingAttendance = await _context.ChamCongs.FirstOrDefaultAsync(
                                cc => cc.MaNv == employee.MaNv && cc.NgayLamViec == holiday.NgayLe1);
                            if (existingAttendance == null)
                            {
                                var attendance = new ChamCong
                                {
                                    MaNv = employee.MaNv,
                                    NgayLamViec = holiday.NgayLe1,
                                    GioVao = new TimeOnly(8, 0, 0),
                                    GioRa = new TimeOnly(17, 0, 0),
                                    TongGio = 8.0m,
                                    TrangThai = "CC3",
                                    GhiChu = $"Ngày lễ: {holiday.TenNgayLe} - Được duyệt bởi: {approverName}"
                                };
                                _context.ChamCongs.Add(attendance);
                            }
                        }
                    }
                    else if (holiday.TrangThai == TrangThai.NL2)
                    {
                        holiday.TrangThai = TrangThai.NL5;
                    }
                }
                else if (holiday.TrangThai == TrangThai.NL3)
                {
                    holiday.TrangThai = TrangThai.NL4;
                    var allEmployees = await _context.NhanViens.ToListAsync();
                    foreach (var employee in allEmployees)
                    {
                        var existingAttendance = await _context.ChamCongs.FirstOrDefaultAsync(
                            cc => cc.MaNv == employee.MaNv && cc.NgayLamViec == holiday.NgayLe1);
                        if (existingAttendance == null)
                        {
                            var attendance = new ChamCong
                            {
                                MaNv = employee.MaNv,
                                NgayLamViec = holiday.NgayLe1,
                                GioVao = new TimeOnly(8, 0, 0),
                                GioRa = new TimeOnly(17, 0, 0),
                                TongGio = 8.0m,
                                TrangThai = "CC3",
                                GhiChu = $"Ngày nghỉ bù: {holiday.TenNgayLe} - Được duyệt bởi: {approverName}"
                            };
                            _context.ChamCongs.Add(attendance);
                        }
                    }
                }

                holiday.MoTa = $"{holiday.MoTa ?? ""}\nĐược duyệt bởi: {approverName} ({approverMaNV}) vào ngày {DateTime.Now:dd/MM/yyyy HH:mm}";

                await _context.SaveChangesAsync();
                

                return Ok(new { success = true, message = $"Ngày lễ ID {id} đã được duyệt bởi {approverName} và thông báo đã được gửi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi duyệt ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("reject/{id}")]
        
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var rejecterMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                var rejecterName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

                if (string.IsNullOrEmpty(rejecterMaNV))
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người từ chối." });
                }

                var holiday = await _context.NgayLes.FindAsync(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                holiday.TrangThai = TrangThai.NL6;
                holiday.MoTa = $"{holiday.MoTa ?? ""}\nBị từ chối bởi: {rejecterName} ({rejecterMaNV}) vào ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Ngày lễ ID {id} đã bị từ chối bởi {rejecterName}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi từ chối ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("cancel/{id}")]
        
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var holiday = await _context.NgayLes.FindAsync(id);
                if (holiday == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ngày lễ." });
                }

                if (holiday.TenNgayLe.Contains("Nghỉ bù"))
                {
                    holiday.TrangThai = TrangThai.NL3;
                }
                else
                {
                    var dayOfWeek = holiday.NgayLe1.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                    holiday.TrangThai = (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                        ? TrangThai.NL2
                        : TrangThai.NL1;
                }

                var relatedAttendances = await _context.ChamCongs
                    .Where(cc => cc.NgayLamViec == holiday.NgayLe1)
                    .ToListAsync();
                _context.ChamCongs.RemoveRange(relatedAttendances);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"Ngày lễ ID {id} đã được hủy và khôi phục về trạng thái ban đầu." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi hủy ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("approve/year/{year}")]
        
        public async Task<IActionResult> ApproveAllByYear(int year)
        {
            try
            {
                var approverMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                var approverName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

                if (string.IsNullOrEmpty(approverMaNV))
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
                }

                var holidaysToApprove = await _context.NgayLes
                    .Where(h => h.NgayLe1.Year == year &&
                               (h.TrangThai == TrangThai.NL1 ||
                                h.TrangThai == TrangThai.NL2 ||
                                h.TrangThai == TrangThai.NL3))
                    .ToListAsync();

                if (holidaysToApprove.Count == 0)
                {
                    return Ok(new { success = true, message = $"Không có ngày lễ nào trong năm {year} cần duyệt." });
                }

                foreach (var holiday in holidaysToApprove)
                {
                    if (holiday.TrangThai == TrangThai.NL1 || holiday.TrangThai == TrangThai.NL3)
                    {
                        holiday.TrangThai = TrangThai.NL4;
                        var allEmployees = await _context.NhanViens.ToListAsync();
                        foreach (var employee in allEmployees)
                        {
                            var existingAttendance = await _context.ChamCongs.FirstOrDefaultAsync(
                                cc => cc.MaNv == employee.MaNv && cc.NgayLamViec == holiday.NgayLe1);
                            if (existingAttendance == null)
                            {
                                var attendance = new ChamCong
                                {
                                    MaNv = employee.MaNv,
                                    NgayLamViec = holiday.NgayLe1,
                                    GioVao = new TimeOnly(8, 0, 0),
                                    GioRa = new TimeOnly(17, 0, 0),
                                    TongGio = 8.0m,
                                    TrangThai = "CC3",
                                    GhiChu = $"Ngày lễ: {holiday.TenNgayLe} - Được duyệt bởi: {approverName}"
                                };
                                _context.ChamCongs.Add(attendance);
                            }
                        }
                    }
                    else if (holiday.TrangThai == TrangThai.NL2)
                    {
                        holiday.TrangThai = TrangThai.NL5;
                    }

                    holiday.MoTa = $"{holiday.MoTa ?? ""}\nĐược duyệt bởi: {approverName} ({approverMaNV}) vào ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
                }
                await _context.SaveChangesAsync();

                

                return Ok(new { success = true, message = $"{holidaysToApprove.Count} ngày lễ trong năm {year} đã được duyệt bởi {approverName} và thông báo đã được gửi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi duyệt các ngày lễ.", error = ex.Message });
            }
        }

        [HttpGet("requests")]
       
        public async Task<ActionResult<IEnumerable<YeuCau>>> GetRequests()
        {
            try
            {
                var requests = await _context.YeuCaus
                    .Where(y => y.TenYeuCau == "Yêu cầu mở duyệt ngày lễ")
                    .OrderByDescending(y => y.NgayTao)
                    .ToListAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tải danh sách yêu cầu.", error = ex.Message });
            }
        }

        [HttpPost("requests/approve/{id}")]
        
        public async Task<IActionResult> ApproveRequest(int id)
        {
            try
            {
                var approverMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                var approverName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

                if (string.IsNullOrEmpty(approverMaNV))
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
                }

                var request = await _context.YeuCaus.FindAsync(id);
                if (request == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy yêu cầu." });
                }

                if (request.TrangThai)
                {
                    return BadRequest(new { success = false, message = "Yêu cầu đã được xử lý trước đó." });
                }

                request.TrangThai = true;
                request.MaNvDuyet = approverMaNV;
                request.NgayDuyet = DateTime.Now;

                await _context.SaveChangesAsync();

                

                return Ok(new { success = true, message = $"Yêu cầu ID {id} đã được duyệt bởi {approverName}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi duyệt yêu cầu.", error = ex.Message });
            }
        }

        [HttpPost("requests/reject/{id}")]
       
        public async Task<IActionResult> RejectRequest(int id)
        {
            try
            {
                var rejecterMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                var rejecterName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

                if (string.IsNullOrEmpty(rejecterMaNV))
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người từ chối." });
                }

                var request = await _context.YeuCaus.FindAsync(id);
                if (request == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy yêu cầu." });
                }

                if (request.TrangThai)
                {
                    return BadRequest(new { success = false, message = "Yêu cầu đã được xử lý trước đó." });
                }

                request.TrangThai = true;
                request.MaNvDuyet = rejecterMaNV;
                request.NgayDuyet = DateTime.Now;

                await _context.SaveChangesAsync();

                

                return Ok(new { success = true, message = $"Yêu cầu ID {id} đã bị từ chối bởi {rejecterName}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi từ chối yêu cầu.", error = ex.Message });
            }
        }

        [HttpPost("requests/cancel/{id}")]
        
        public async Task<IActionResult> CancelRequest(int id)
        {
            try
            {
                var cancellerMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
                var cancellerName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

                if (string.IsNullOrEmpty(cancellerMaNV))
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy thông tin người hủy." });
                }

                var request = await _context.YeuCaus.FindAsync(id);
                if (request == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy yêu cầu." });
                }

                if (!request.TrangThai)
                {
                    return BadRequest(new { success = false, message = "Yêu cầu chưa được xử lý, không thể hủy." });
                }

                request.TrangThai = false;
                request.MaNvDuyet = null;
                request.NgayDuyet = null;

                await _context.SaveChangesAsync();

                

                return Ok(new { success = true, message = $"Yêu cầu ID {id} đã được hủy bởi {cancellerName} và trạng thái trở về 'Chờ xử lý'." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi hủy yêu cầu.", error = ex.Message });
            }
        }

        
    }
}