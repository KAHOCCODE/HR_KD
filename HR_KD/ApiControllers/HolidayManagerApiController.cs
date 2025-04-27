using HR_KD.Data;
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
        public ActionResult<IEnumerable<NgayLe>> GetHolidays(int? year)
        {
            IQueryable<NgayLe> holidays = _context.NgayLes;

            if (year.HasValue)
            {
                holidays = holidays.Where(h => h.NgayLe1.Year == year.Value);
            }

            return holidays.ToListAsync().Result; // Sử dụng Result() để đồng bộ hóa cho đơn giản trong ví dụ
        }

        [HttpGet("years")]
        public ActionResult<IEnumerable<int>> GetHolidayYears()
        {
            return _context.NgayLes.Select(h => h.NgayLe1.Year).Distinct().OrderByDescending(y => y).ToListAsync().Result;
        }

        [HttpGet("{id}")]
        public ActionResult<NgayLe> GetHoliday(int id)
        {
            var holiday = _context.NgayLes.FindAsync(id).Result;
            if (holiday == null)
            {
                return NotFound();
            }
            return holiday;
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var holiday = await _context.NgayLes.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            if (holiday.TrangThai != "Đã duyệt")
            {
                holiday.TrangThai = "Đã duyệt";
                await _context.SaveChangesAsync();

                // Cập nhật chấm công
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
                            TrangThai = "Đã duyệt",
                            GhiChu = $"Ngày lễ: {holiday.TenNgayLe}"
                        };
                        _context.ChamCongs.Add(attendance);
                    }
                }
                await _context.SaveChangesAsync();

                // Gửi email thông báo cho ngày lễ vừa duyệt
                await SendHolidayApprovalNotification(holiday);

                return Ok($"Ngày lễ ID {id} đã được duyệt và thông báo đã được gửi.");
            }
            return Ok($"Ngày lễ ID {id} đã được duyệt trước đó.");
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            var holiday = await _context.NgayLes.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            holiday.TrangThai = "Đã từ chối";
            await _context.SaveChangesAsync();

            return Ok($"Ngày lễ ID {id} đã bị từ chối.");
        }

        [HttpPost("approve/year/{year}")]
        public async Task<IActionResult> ApproveAllByYear(int year)
        {
            var holidaysToApprove = await _context.NgayLes
                .Where(h => h.NgayLe1.Year == year && h.TrangThai != "Đã duyệt")
                .ToListAsync();

            if (holidaysToApprove.Count == 0)
            {
                return Ok($"Không có ngày lễ nào trong năm {year} cần duyệt.");
            }

            foreach (var holiday in holidaysToApprove)
            {
                holiday.TrangThai = "Đã duyệt";

                // Cập nhật chấm công
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
                            TrangThai = "Đã duyệt",
                            GhiChu = $"Ngày lễ: {holiday.TenNgayLe}"
                        };
                        _context.ChamCongs.Add(attendance);
                    }
                }
            }
            await _context.SaveChangesAsync();

            // Gửi email thông báo tổng hợp cho tất cả ngày lễ vừa duyệt trong năm
            await SendBulkHolidayApprovalNotification(holidaysToApprove);

            return Ok($"{holidaysToApprove.Count} ngày lễ trong năm {year} đã được duyệt và thông báo đã được gửi.");
        }

        private async Task SendHolidayApprovalNotification(NgayLe holiday)
        {
            var employeesToSend = await _context.NhanViens.Where(e => !string.IsNullOrEmpty(e.Email)).ToListAsync();
            if (employeesToSend.Count == 0) return;

            string subject = $"Thông báo: Ngày lễ '{holiday.TenNgayLe}' đã được duyệt";
            string body = $"Chào bạn,\n\nNgày lễ '{holiday.TenNgayLe}' ({holiday.NgayLe1.ToString("dd/MM/yyyy")}) đã được duyệt.\n\nTrân trọng,\nBan quản lý.";

            await SendEmailAsync(employeesToSend, subject, body);
        }

        private async Task SendBulkHolidayApprovalNotification(List<NgayLe> holidays)
        {
            var employeesToSend = await _context.NhanViens.Where(e => !string.IsNullOrEmpty(e.Email)).ToListAsync();
            if (employeesToSend.Count == 0) return;

            if (holidays.Count == 0) return;

            var latestYear = holidays.First().NgayLe1.Year; // Giả sử tất cả ngày lễ đều trong cùng một năm

            string subject = $"Thông báo: Lịch nghỉ lễ năm {latestYear} đã được duyệt";
            string body = $"Chào bạn,\n\nDưới đây là lịch nghỉ lễ năm {latestYear} đã được duyệt:\n\n";

            foreach (var holiday in holidays)
            {
                body += $"{holiday.TenNgayLe}: {holiday.NgayLe1.ToString("dd/MM/yyyy")}\n";
            }

            body += "\nTrân trọng,\nBan quản lý.";

            await SendEmailAsync(employeesToSend, subject, body);
        }

        private async Task SendEmailAsync(List<NhanVien> recipients, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPortString = _configuration["EmailSettings:SmtpPort"]; // Lấy giá trị dưới dạng string
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"]; // Đã sửa theo cấu hình chuẩn
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"]; // Đã sửa theo cấu hình chuẩn
            var fromEmail = _configuration["EmailSettings:FromEmail"];     // Thêm nếu bạn dùng
            var displayName = _configuration["EmailSettings:DisplayName"]; // Thêm nếu bạn dùng

            // Kiểm tra cấu hình quan trọng
            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpPortString) ||
                string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                Console.WriteLine("Lỗi cấu hình: Thiếu thông tin cấu hình email quan trọng.");
                return; // Hoặc ném một exception phù hợp
            }

            if (!int.TryParse(smtpPortString, out var smtpPort))
            {
                Console.WriteLine($"Lỗi cấu hình: Giá trị SmtpPort không hợp lệ: '{smtpPortString}'.");
                return; // Hoặc ném một exception
            }

            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                foreach (var employee in recipients)
                {
                    try
                    {
                        MailMessage mailMessage = new MailMessage(
                            new MailAddress(fromEmail, displayName ?? "Ban quản lý"), //Sử dụng displayName nếu có
                            new MailAddress(employee.Email)
                        );
                        mailMessage.Subject = subject;
                        mailMessage.Body = body;
                        mailMessage.IsBodyHtml = false;

                        await smtpClient.SendMailAsync(mailMessage);
                        Console.WriteLine($"Đã gửi email '{subject}' đến: {employee.Email}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi gửi email '{subject}' đến {employee.Email}: {ex.Message}");
                        // Ghi log lỗi chi tiết hơn ở đây (ví dụ: sử dụng ILogger)
                    }
                }
            }
        }

    }
}
