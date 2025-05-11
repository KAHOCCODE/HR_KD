using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
namespace HR_KD.ApiControllers
{
    [Route("api/Holidays")]
    [ApiController]
    public class HolidayApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly IConfiguration _configuration; // Inject configuration for email settings

        public HolidayApiController(HrDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpPost("Add")]
        public IActionResult AddHoliday(HolidayDTO holidayDto)
        {
            if (holidayDto == null || string.IsNullOrEmpty(holidayDto.TenNgayLe) || holidayDto.NgayLe1 == default)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            // Kiểm tra thời gian hiện tại có nằm trong khoảng 1/12-30/12 không
            var currentDate = DateTime.Now;
            if (currentDate.Month != 12 || currentDate.Day < 1 || currentDate.Day > 30)
            {
                return BadRequest(new { success = false, message = "Chức năng thêm ngày lễ chỉ được mở trong khoảng thời gian từ 1/12 đến 30/12 hàng năm." });
            }

            var existingHoliday = _context.NgayLes
                .FirstOrDefault(h => h.NgayLe1 == DateOnly.FromDateTime(holidayDto.NgayLe1));

            if (existingHoliday != null)
            {
                return BadRequest(new { success = false, message = "Ngày lễ này đã tồn tại trong hệ thống." });
            }

            // Xác định trạng thái ngày lễ
            string trangThai;
            var holidayDate = DateOnly.FromDateTime(holidayDto.NgayLe1);
            var dayOfWeek = holidayDate.ToDateTime(TimeOnly.MinValue).DayOfWeek;

            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
            {
                trangThai = TrangThai.NL2; // Ngày lễ rơi vào cuối tuần
            }
            else if (holidayDto.TenNgayLe.Contains("Nghỉ bù"))
            {
                trangThai = TrangThai.NL3; // Ngày nghỉ bù
            }
            else
            {
                trangThai = TrangThai.NL1; // Ngày lễ thường
            }

            var holiday = new NgayLe
            {
                TenNgayLe = holidayDto.TenNgayLe,
                NgayLe1 = holidayDate,
                SoNgayNghi = holidayDto.SoNgayNghi,
                MoTa = holidayDto.MoTa,
                TrangThai = trangThai
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
                // Kiểm tra thời gian hiện tại có nằm trong khoảng 1/12-30/12 không
                var currentDate = DateTime.Now;
                if (currentDate.Month != 12 || currentDate.Day < 1 || currentDate.Day > 30)
                {
                    return BadRequest(new { success = false, message = "Chức năng xóa ngày lễ chỉ được mở trong khoảng thời gian từ 1/12 đến 30/12 hàng năm." });
                }

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

                // Gửi email thông báo hủy ngày lễ
                SendHolidayCancellationEmail(holiday);

                return Ok(new
                {
                    success = true,
                    message = "Đã hủy ngày lễ, trạng thái chuyển về 'Chờ duyệt' và tất cả chấm công liên quan đã bị xóa.",
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
        [Authorize(Policy = "IsDirector")]
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
                SendHolidayApprovalEmail(holiday); // Gửi email thông báo duyệt ngày lễ

                return Ok(new { success = true, message = "Ngày lễ đã được duyệt." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi duyệt ngày lễ.", error = ex.Message });
            }
        }

        [HttpPost("Reject/{id}")]
        [Authorize(Policy = "IsDirector")]
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
        [Authorize(Policy = "IsDirector")]
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
                SendHolidayApprovalEmailForYear(holidaysToApprove, year);
                return Ok($"Đã duyệt {holidaysToApprove.Count} ngày lễ trong năm {year}.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi duyệt ngày lễ.", error = ex.Message });
            }
        }
        private void SendHolidayCancellationEmail(NgayLe holiday)
        {
            try
            {
                // Lấy thông tin cấu hình email từ appsettings.json
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]); // Đã sửa ở đây
                var smtpUsername = _configuration["EmailSettings:SenderEmail"]; // Đã sửa ở đây
                var smtpPassword = _configuration["EmailSettings:SenderPassword"];
                var fromEmail = _configuration["EmailSettings:SenderEmail"];   // Đã sửa ở đây

                // Lấy danh sách email của tất cả nhân viên
                var employeeEmails = _context.NhanViens
                    .Where(nv => !string.IsNullOrEmpty(nv.Email))
                    .Select(nv => nv.Email)
                    .ToList();

                if (!employeeEmails.Any())
                {
                    // Không có email nhân viên, không gửi email
                    return;
                }

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(fromEmail);
                        foreach (var toEmail in employeeEmails)
                        {
                            mailMessage.To.Add(toEmail);
                        }
                        mailMessage.Subject = $"Thông báo: Hủy ngày lễ '{holiday.TenNgayLe}'";
                        mailMessage.Body = $"Kính gửi Quý nhân viên,\n\nChúng tôi xin thông báo rằng ngày lễ '{holiday.TenNgayLe}' vào ngày {holiday.NgayLe1.ToString("dd/MM/yyyy")} đã bị hủy.\n\nTrân trọng,\nBan Quản Lý";
                        mailMessage.IsBodyHtml = false;

                        client.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi gửi email (quan trọng để theo dõi nếu có vấn đề)
                Console.WriteLine($"Lỗi khi gửi email hủy ngày lễ: {ex.Message}");
                // Không ném lại exception để không ảnh hưởng đến quá trình hủy ngày lễ chính
            }
        }
        private void SendHolidayApprovalEmail(NgayLe holiday)
        {
            try
            {
                // Lấy thông tin cấu hình email từ appsettings.json
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                var smtpUsername = _configuration["EmailSettings:SenderEmail"];
                var smtpPassword = _configuration["EmailSettings:SenderPassword"];
                var fromEmail = _configuration["EmailSettings:SenderEmail"];

                // Lấy danh sách email của tất cả nhân viên
                var employeeEmails = _context.NhanViens
                    .Where(nv => !string.IsNullOrEmpty(nv.Email))
                    .Select(nv => nv.Email)
                    .ToList();

                if (!employeeEmails.Any())
                {
                    // Không có email nhân viên, không gửi email
                    return;
                }

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(fromEmail);
                        foreach (var toEmail in employeeEmails)
                        {
                            mailMessage.To.Add(toEmail);
                        }
                        mailMessage.Subject = $"Thông báo: Duyệt ngày lễ '{holiday.TenNgayLe}'"; // Chủ đề email
                        mailMessage.Body = $"Kính gửi Quý nhân viên,\n\nChúng tôi xin thông báo rằng ngày lễ '{holiday.TenNgayLe}' vào ngày {holiday.NgayLe1.ToString("dd/MM/yyyy")} đã được duyệt.\n\nTrân trọng,\nBan Quản Lý"; // Nội dung email
                        mailMessage.IsBodyHtml = false;

                        client.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi gửi email
                Console.WriteLine($"Lỗi khi gửi email duyệt ngày lễ: {ex.Message}");
            }
        }
        private void SendHolidayApprovalEmailForYear(List<NgayLe> holidays, int year)
        {
            try
            {
                // Lấy thông tin cấu hình email từ appsettings.json
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
                var smtpUsername = _configuration["EmailSettings:SenderEmail"];
                var smtpPassword = _configuration["EmailSettings:SenderPassword"];
                var fromEmail = _configuration["EmailSettings:SenderEmail"];

                // Lấy danh sách email của tất cả nhân viên
                var employeeEmails = _context.NhanViens
                    .Where(nv => !string.IsNullOrEmpty(nv.Email))
                    .Select(nv => nv.Email)
                    .ToList();

                if (!employeeEmails.Any())
                {
                    // Không có email nhân viên, không gửi email
                    return;
                }

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(fromEmail);
                        foreach (var toEmail in employeeEmails)
                        {
                            mailMessage.To.Add(toEmail);
                        }
                        mailMessage.Subject = $"Thông báo: Duyệt các ngày lễ năm {year}"; // Chủ đề email
                        StringBuilder body = new StringBuilder();
                        body.AppendLine($"Kính gửi Quý nhân viên,\n\nChúng tôi xin thông báo rằng các ngày lễ trong năm {year} đã được duyệt:");

                        foreach (var holiday in holidays)
                        {
                            body.AppendLine($"- {holiday.TenNgayLe} vào ngày {holiday.NgayLe1.ToString("dd/MM/yyyy")}");
                        }

                        body.AppendLine("\n\nTrân trọng,\nBan Quản Lý");
                        mailMessage.Body = body.ToString();
                        mailMessage.IsBodyHtml = false;

                        client.Send(mailMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi gửi email
                Console.WriteLine($"Lỗi khi gửi email duyệt ngày lễ: {ex.Message}");
            }
        }

    }
}