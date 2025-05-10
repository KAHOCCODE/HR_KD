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
        public async Task<ActionResult<IEnumerable<NgayLe>>> GetHolidays(int? year)
        {
            IQueryable<NgayLe> holidays = _context.NgayLes;

            if (year.HasValue)
            {
                holidays = holidays.Where(h => h.NgayLe1.Year == year.Value);
            }

            return await holidays.ToListAsync();
        }

        [HttpGet("years")]
        public async Task<ActionResult<IEnumerable<int>>> GetHolidayYears()
        {
            return await _context.NgayLes
                .Select(h => h.NgayLe1.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NgayLe>> GetHoliday(int id)
        {
            var holiday = await _context.NgayLes.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }
            return holiday;
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            // Lấy thông tin người duyệt từ claims
            var approverMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
            var approverName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

            if (string.IsNullOrEmpty(approverMaNV))
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin người duyệt." });
            }

            var holiday = await _context.NgayLes.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            // Kiểm tra trạng thái hiện tại
            if (holiday.TrangThai == TrangThai.NL4 || holiday.TrangThai == TrangThai.NL5)
            {
                return Ok($"Ngày lễ ID {id} đã được duyệt trước đó.");
            }

            // Xử lý duyệt ngày lễ
            if (holiday.TrangThai == TrangThai.NL1 || holiday.TrangThai == TrangThai.NL2)
            {
                // Nếu là ngày lễ thường hoặc ngày lễ cuối tuần
                if (holiday.TrangThai == TrangThai.NL1)
                {
                    holiday.TrangThai = TrangThai.NL4; // Duyệt và tạo chấm công
                    // Tạo chấm công cho tất cả nhân viên
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
                    holiday.TrangThai = TrangThai.NL5; // Duyệt nhưng không tạo chấm công
                }
            }
            else if (holiday.TrangThai == TrangThai.NL3)
            {
                // Nếu là ngày nghỉ bù, xử lý như ngày lễ thường
                holiday.TrangThai = TrangThai.NL4;
                // Tạo chấm công cho tất cả nhân viên
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

            // Cập nhật thông tin người duyệt
            holiday.MoTa = $"{holiday.MoTa}\nĐược duyệt bởi: {approverName} ({approverMaNV}) vào ngày {DateTime.Now:dd/MM/yyyy HH:mm}";

            await _context.SaveChangesAsync();
            await SendHolidayApprovalNotification(holiday);

            return Ok($"Ngày lễ ID {id} đã được duyệt bởi {approverName} và thông báo đã được gửi.");
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> Reject(int id)
        {
            // Lấy thông tin người từ chối từ claims
            var rejecterMaNV = User.Claims.FirstOrDefault(c => c.Type == "MaNV")?.Value;
            var rejecterName = User.Claims.FirstOrDefault(c => c.Type == "TenNV")?.Value;

            if (string.IsNullOrEmpty(rejecterMaNV))
            {
                return BadRequest(new { success = false, message = "Không tìm thấy thông tin người từ chối." });
            }

            var holiday = await _context.NgayLes.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            holiday.TrangThai = TrangThai.NL6; // Đánh dấu là ngày lễ bị từ chối
            holiday.MoTa = $"{holiday.MoTa}\nBị từ chối bởi: {rejecterName} ({rejecterMaNV}) vào ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
            await _context.SaveChangesAsync();

            return Ok($"Ngày lễ ID {id} đã bị từ chối bởi {rejecterName}.");
        }

        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var holiday = await _context.NgayLes.FindAsync(id);
            if (holiday == null)
            {
                return NotFound();
            }

            // Khôi phục trạng thái ban đầu dựa vào tên ngày lễ
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

            // Xóa các bản ghi chấm công liên quan
            var relatedAttendances = await _context.ChamCongs
                .Where(cc => cc.NgayLamViec == holiday.NgayLe1)
                .ToListAsync();
            _context.ChamCongs.RemoveRange(relatedAttendances);

            await _context.SaveChangesAsync();

            return Ok($"Ngày lễ ID {id} đã được hủy và khôi phục về trạng thái ban đầu.");
        }

        [HttpPost("approve/year/{year}")]
        public async Task<IActionResult> ApproveAllByYear(int year)
        {
            // Lấy thông tin người duyệt từ claims
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
                return Ok($"Không có ngày lễ nào trong năm {year} cần duyệt.");
            }

            foreach (var holiday in holidaysToApprove)
            {
                if (holiday.TrangThai == TrangThai.NL1 || holiday.TrangThai == TrangThai.NL3)
                {
                    holiday.TrangThai = TrangThai.NL4;
                    // Tạo chấm công cho tất cả nhân viên
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

                // Cập nhật thông tin người duyệt
                holiday.MoTa = $"{holiday.MoTa}\nĐược duyệt bởi: {approverName} ({approverMaNV}) vào ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
            await _context.SaveChangesAsync();

            await SendBulkHolidayApprovalNotification(holidaysToApprove);

            return Ok($"{holidaysToApprove.Count} ngày lễ trong năm {year} đã được duyệt bởi {approverName} và thông báo đã được gửi.");
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
            if (employeesToSend.Count == 0 || holidays.Count == 0) return;

            var latestYear = holidays.First().NgayLe1.Year;

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
            var smtpPortString = _configuration["EmailSettings:SmtpPort"];
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var displayName = _configuration["EmailSettings:DisplayName"];

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpPortString) ||
                string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                Console.WriteLine("Lỗi cấu hình: Thiếu thông tin cấu hình email quan trọng.");
                return;
            }

            if (!int.TryParse(smtpPortString, out var smtpPort))
            {
                Console.WriteLine($"Lỗi cấu hình: Giá trị SmtpPort không hợp lệ: '{smtpPortString}'.");
                return;
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
                            new MailAddress(fromEmail, displayName ?? "Ban quản lý"),
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
                    }
                }
            }
        }

        [HttpPost("send-yearly-notification")]
        public async Task<IActionResult> SendYearlyHolidayNotification()
        {
            var currentYear = DateTime.Now.Year;
            var approvedHolidays = await _context.NgayLes
                .Where(h => h.NgayLe1.Year == currentYear && 
                           (h.TrangThai == TrangThai.NL4 || h.TrangThai == TrangThai.NL5))
                .OrderBy(h => h.NgayLe1)
                .ToListAsync();

            if (approvedHolidays.Count == 0)
            {
                return Ok($"Không có ngày lễ nào đã được duyệt cho năm {currentYear}.");
            }

            var employeesToSend = await _context.NhanViens
                .Where(e => !string.IsNullOrEmpty(e.Email))
                .ToListAsync();

            if (employeesToSend.Count == 0)
            {
                return Ok("Không tìm thấy nhân viên nào có email để gửi thông báo.");
            }

            string subject = $"Thông báo: Lịch nghỉ lễ năm {currentYear}";
            string body = $"Chào bạn,\n\nDưới đây là lịch nghỉ lễ năm {currentYear} đã được duyệt:\n\n";

            foreach (var holiday in approvedHolidays)
            {
                string holidayType = holiday.TrangThai == TrangThai.NL4 ? "Ngày lễ" : "Ngày nghỉ cuối tuần";
                body += $"{holidayType}: {holiday.TenNgayLe} - {holiday.NgayLe1.ToString("dd/MM/yyyy")}\n";
            }

            body += "\nTrân trọng,\nBan quản lý.";

            await SendEmailAsync(employeesToSend, subject, body);

            return Ok($"Đã gửi thông báo lịch nghỉ lễ năm {currentYear} cho {employeesToSend.Count} nhân viên.");
        }
    }
}