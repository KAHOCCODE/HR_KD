using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using System.Linq;
using HR_KD.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

[Route("api/AttendanceRequestManager")]
[ApiController]
public class AttendanceRequestManagerApiController : ControllerBase
{
    private readonly HrDbContext _context;
    private readonly IConfiguration _configuration;

    public AttendanceRequestManagerApiController(HrDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private int? GetMaNvFromClaims()
    {
        var maNvClaim = User.FindFirst("MaNV")?.Value;
        return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
    }

    // 🔹 Lấy danh sách phòng ban
    [HttpGet("GetDepartmentsManager")]
    public IActionResult GetDepartments()
    {
        var departments = _context.PhongBans
            .Select(pb => new { pb.MaPhongBan, pb.TenPhongBan })
            .ToList();
        return Ok(departments);
    }

    // 🔹 Lấy danh sách chức vụ
    [HttpGet("GetPositionsManager")]
    public IActionResult GetPositions()
    {
        var positions = _context.ChucVus
            .Select(cv => new { cv.MaChucVu, cv.TenChucVu })
            .ToList();
        return Ok(positions);
    }

    // 🔹 Lấy danh sách nhân viên theo phòng ban và chức vụ
    [HttpGet("GetEmployeesManager")]
    public IActionResult GetEmployees(int? maPhongBan, int? maChucVu)
    {
        var employees = _context.NhanViens.AsQueryable();
        if (maPhongBan.HasValue)
        {
            employees = employees.Where(nv => nv.MaPhongBan == maPhongBan.Value);
        }
        if (maChucVu.HasValue)
        {
            employees = employees.Where(nv => nv.MaChucVu == maChucVu.Value);
        }
        var result = employees
            .Select(nv => new { nv.MaNv, nv.HoTen })
            .ToList();
        return Ok(result);
    }

    // 🔹 Lấy danh sách chấm công của nhân viên
    [HttpGet("GetAttendanceManagerRecords")]
    public IActionResult GetAttendanceRecords(int maNv)
    {
        var records = _context.ChamCongs
            .Where(cc => cc.MaNv == maNv)
            .Select(cc => new
            {
                cc.MaChamCong,
                cc.NgayLamViec,
                cc.GioVao,
                cc.GioRa,
                cc.TongGio,
                TrangThai = cc.TrangThai ?? "Chờ duyệt",
                cc.GhiChu
            })
            .ToList();
        return Ok(new { success = true, records });
    }
   
    // 🔹 Lấy danh sách nhân viên chưa chấm công đủ trong tuần
    [HttpGet("GetMissingAttendance")]
    public IActionResult GetMissingAttendance(DateTime startDate, DateTime endDate)
    {
        var startOfWeek = DateOnly.FromDateTime(startDate);
        var endOfWeek = DateOnly.FromDateTime(endDate);

        // Lấy tất cả nhân viên
        var employees = _context.NhanViens
            .Include(nv => nv.MaPhongBanNavigation)
            .ToList();

        var result = new List<object>();

        foreach (var employee in employees)
        {
            // Lấy các ngày làm việc trong tuần (T2-T6)
            var workDays = new List<DateOnly>();
            for (var date = startOfWeek; date <= endOfWeek; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Sunday && date.DayOfWeek != DayOfWeek.Saturday)
                {
                    workDays.Add(date);
                }
            }

            // Lấy các ngày đã chấm công
            var attendanceDays = _context.ChamCongs
                .Where(cc => cc.MaNv == employee.MaNv && 
                            cc.NgayLamViec >= startOfWeek && 
                            cc.NgayLamViec <= endOfWeek)
                .Select(cc => cc.NgayLamViec)
                .ToList();

            // Tìm các ngày thiếu
            var missingDays = workDays.Where(day => !attendanceDays.Contains(day)).ToList();

            if (missingDays.Any())
            {
                result.Add(new
                {
                    maNv = employee.MaNv,
                    hoTen = employee.HoTen,
                    tenPhongBan = employee.MaPhongBanNavigation.TenPhongBan,
                    ngayThieu = missingDays.Select(d => d.ToString("dd/MM/yyyy")).ToList(),
                    trangThai = "Chưa chấm công đủ"
                });
            }
        }

        return Ok(result);
    }

    // 🔹 Lấy danh sách nhân viên cần làm bù
    [HttpGet("GetMakeupHours")]
    public IActionResult GetMakeupHours(DateTime startDate, DateTime endDate)
    {
        var startOfWeek = DateOnly.FromDateTime(startDate);
        var endOfWeek = DateOnly.FromDateTime(endDate);

        var result = _context.TongGioThieus
            .Include(t => t.MaNvNavigation)
            .ThenInclude(nv => nv.MaPhongBanNavigation)
            .Where(t => t.TongGioConThieu > 0)
            .Select(t => new
            {
                maNv = t.MaNv,
                hoTen = t.MaNvNavigation.HoTen,
                tenPhongBan = t.MaNvNavigation.MaPhongBanNavigation.TenPhongBan,
                tongGioThieu = t.TongGioConThieu,
                tongGioDaBu = t.TongGioLamBu,
                soGioCanBu = t.TongGioConThieu - t.TongGioLamBu,
                thoiHan = t.NgayKetThucThieu.ToString("dd/MM/yyyy")
            })
            .Where(x => x.soGioCanBu > 0)
            .ToList();

        return Ok(result);
    }

    // 🔹 Lấy danh sách yêu cầu tăng ca của nhân viên
    [HttpGet("GetOvertimeRequests")]
    public IActionResult GetOvertimeRequests(int maNv, DateTime startDate, DateTime endDate)
    {
        var startOfWeek = DateOnly.FromDateTime(startDate);
        var endOfWeek = DateOnly.FromDateTime(endDate);

        var requests = _context.TangCas
            .Include(tc => tc.MaNvNavigation)
            .ThenInclude(nv => nv.MaPhongBanNavigation)
            .Where(tc => tc.MaNv == maNv && 
                        tc.NgayTangCa >= startOfWeek && 
                        tc.NgayTangCa <= endOfWeek)
            .Select(tc => new
            {
                maNv = tc.MaNv,
                hoTen = tc.MaNvNavigation.HoTen,
                tenPhongBan = tc.MaNvNavigation.MaPhongBanNavigation.TenPhongBan,
                tuanTangCa = $"{tc.NgayTangCa:dd/MM/yyyy}",
                soGio = tc.SoGioTangCa,
                trangThai = tc.TrangThai
            })
            .ToList();

        return Ok(requests);
    }

    // 🔹 Gửi email nhắc nhở chấm công
    [HttpPost("SendAttendanceReminders")]
    public IActionResult SendAttendanceReminders([FromBody] DateRequestDTO request)
    {
        try
        {
            var startDate = DateOnly.FromDateTime(DateTime.Parse(request.startDate));
            var endDate = startDate.AddDays(6);

            var missingAttendance = GetMissingAttendance(startDate.ToDateTime(TimeOnly.MinValue), 
                                                       endDate.ToDateTime(TimeOnly.MinValue));
            var employees = ((OkObjectResult)missingAttendance).Value as List<object>;

            // Lọc danh sách nhân viên theo danh sách được chọn
            var selectedEmployees = employees
                .Where(emp => request.selectedEmployees.Any(se => se.maNv == ((dynamic)emp).maNv))
                .ToList();

            foreach (var emp in selectedEmployees)
            {
                var selectedEmp = request.selectedEmployees.First(se => se.maNv == ((dynamic)emp).maNv);
                SendAttendanceReminderEmail(selectedEmp.email, ((dynamic)emp).hoTen, 
                                         ((dynamic)emp).ngayThieu as List<string>);
            }

            return Ok(new { success = true, message = $"Đã gửi email nhắc nhở chấm công cho {selectedEmployees.Count} nhân viên." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 🔹 Gửi email nhắc nhở làm bù
    [HttpPost("SendMakeupReminders")]
    public IActionResult SendMakeupReminders([FromBody] DateRequestDTO request)
    {
        try
        {
            var startDate = DateOnly.FromDateTime(DateTime.Parse(request.startDate));
            var endDate = startDate.AddDays(6);

            var makeupHours = GetMakeupHours(startDate.ToDateTime(TimeOnly.MinValue), 
                                           endDate.ToDateTime(TimeOnly.MinValue));
            var employees = ((OkObjectResult)makeupHours).Value as List<object>;

            // Lọc danh sách nhân viên theo danh sách được chọn
            var selectedEmployees = employees
                .Where(emp => request.selectedEmployees.Any(se => se.maNv == ((dynamic)emp).maNv))
                .ToList();

            foreach (var emp in selectedEmployees)
            {
                var selectedEmp = request.selectedEmployees.First(se => se.maNv == ((dynamic)emp).maNv);
                SendMakeupReminderEmail(selectedEmp.email, ((dynamic)emp).hoTen, 
                                      ((dynamic)emp).soGioCanBu, 
                                      ((dynamic)emp).thoiHan);
            }

            return Ok(new { success = true, message = $"Đã gửi email nhắc nhở làm bù cho {selectedEmployees.Count} nhân viên." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 🔹 Gửi yêu cầu tăng ca
    [HttpPost("SendOvertimeRequest")]
    public IActionResult SendOvertimeRequest([FromBody] OvertimeRequestDTO request)
    {
        try
        {
            var employee = _context.NhanViens.Find(request.maNv);
            if (employee == null)
            {
                return BadRequest(new { success = false, message = "Không tìm thấy nhân viên." });
            }

            var startDate = DateOnly.FromDateTime(DateTime.Parse(request.startDate));
            var endDate = startDate.AddDays(6);

            // Tạo yêu cầu tăng ca
            var tangCa = new TangCa
            {
                MaNv = request.maNv,
                NgayTangCa = startDate,
                SoGioTangCa = request.soGio,
                TrangThai = "TC1", // Chờ duyệt
                MaNvDuyet = GetMaNvFromClaims() ?? 0 // Lấy mã nhân viên duyệt từ claims
            };

            _context.TangCas.Add(tangCa);
            _context.SaveChanges();

            // Gửi email thông báo
            SendOvertimeRequestEmail(employee.Email, employee.HoTen, startDate, request.soGio);

            return Ok(new { success = true, message = "Đã gửi yêu cầu tăng ca thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // 🔹 Helper methods for sending emails
    private void SendAttendanceReminderEmail(string recipientEmail, string employeeName, List<string> missingDays)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"];
        var senderPassword = emailSettings["SenderPassword"];
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"]);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("HR Department", senderEmail));
        message.To.Add(new MailboxAddress(employeeName, recipientEmail));
        message.Subject = "Nhắc nhở chấm công";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #007bff; color: white; padding: 20px; text-align: center;'>
                    <h2>Nhắc Nhở Chấm Công</h2>
                </div>
                <div style='padding: 20px; background-color: #f8f9fa;'>
                    <p>Kính gửi <strong>{employeeName}</strong>,</p>
                    <p>Hệ thống ghi nhận bạn chưa chấm công đủ trong tuần này.</p>
                    <p>Các ngày cần chấm công:</p>
                    <ul style='list-style-type: none; padding-left: 0;'>
                        {string.Join("", missingDays.Select(d => $"<li style='padding: 5px; background-color: #e9ecef; margin-bottom: 5px; border-radius: 3px;'>{d}</li>"))}
                    </ul>
                    <p>Vui lòng truy cập hệ thống để chấm công đầy đủ.</p>
                    <p>Trân trọng,</p>
                    <p><strong>Phòng Nhân sự</strong></p>
                </div>
                <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                    <p style='color: #6c757d; font-size: 12px;'>Email này được gửi tự động, vui lòng không trả lời trực tiếp.</p>
                </div>
            </div>";

        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
            client.Authenticate(senderEmail, senderPassword);
            client.Send(message);
            client.Disconnect(true);
        }
    }

    private void SendMakeupReminderEmail(string recipientEmail, string employeeName, decimal hours, string deadline)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"];
        var senderPassword = emailSettings["SenderPassword"];
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"]);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("HR Department", senderEmail));
        message.To.Add(new MailboxAddress(employeeName, recipientEmail));
        message.Subject = "Nhắc nhở làm bù";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #ffc107; color: white; padding: 20px; text-align: center;'>
                    <h2>Nhắc Nhở Làm Bù</h2>
                </div>
                <div style='padding: 20px; background-color: #f8f9fa;'>
                    <p>Kính gửi <strong>{employeeName}</strong>,</p>
                    <p>Hệ thống ghi nhận bạn còn <strong>{hours}</strong> giờ cần làm bù.</p>
                    <p>Thời hạn làm bù: <strong>{deadline}</strong></p>
                    <p>Vui lòng truy cập hệ thống để đăng ký làm bù.</p>
                    <p>Trân trọng,</p>
                    <p><strong>Phòng Nhân sự</strong></p>
                </div>
                <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                    <p style='color: #6c757d; font-size: 12px;'>Email này được gửi tự động, vui lòng không trả lời trực tiếp.</p>
                </div>
            </div>";

        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
            client.Authenticate(senderEmail, senderPassword);
            client.Send(message);
            client.Disconnect(true);
        }
    }

    private void SendOvertimeRequestEmail(string recipientEmail, string employeeName, DateOnly startDate, decimal hours)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"];
        var senderPassword = emailSettings["SenderPassword"];
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"]);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("HR Department", senderEmail));
        message.To.Add(new MailboxAddress(employeeName, recipientEmail));
        message.Subject = "Yêu cầu tăng ca";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #28a745; color: white; padding: 20px; text-align: center;'>
                    <h2>Yêu Cầu Tăng Ca</h2>
                </div>
                <div style='padding: 20px; background-color: #f8f9fa;'>
                    <p>Kính gửi <strong>{employeeName}</strong>,</p>
                    <p>Bạn có yêu cầu tăng ca mới:</p>
                    <ul style='list-style-type: none; padding-left: 0;'>
                        <li style='padding: 10px; background-color: #e9ecef; margin-bottom: 5px; border-radius: 3px;'>
                            <strong>Tuần bắt đầu:</strong> {startDate:dd/MM/yyyy}
                        </li>
                        <li style='padding: 10px; background-color: #e9ecef; margin-bottom: 5px; border-radius: 3px;'>
                            <strong>Số giờ tăng ca:</strong> {hours}
                        </li>
                    </ul>
                    <p>Vui lòng truy cập hệ thống để xem chi tiết và phản hồi.</p>
                    <p>Trân trọng,</p>
                    <p><strong>Phòng Nhân sự</strong></p>
                </div>
                <div style='text-align: center; padding: 20px; background-color: #f8f9fa; border-top: 1px solid #dee2e6;'>
                    <p style='color: #6c757d; font-size: 12px;'>Email này được gửi tự động, vui lòng không trả lời trực tiếp.</p>
                </div>
            </div>";

        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
            client.Authenticate(senderEmail, senderPassword);
            client.Send(message);
            client.Disconnect(true);
        }
    }
}

public class DateRequestDTO
{
    public string startDate { get; set; }
    public List<SelectedEmployeeDTO> selectedEmployees { get; set; }
}

public class SelectedEmployeeDTO
{
    public int maNv { get; set; }
    public string email { get; set; }
}

public class OvertimeRequestDTO
{
    public int maNv { get; set; }
    public string startDate { get; set; }
    public decimal soGio { get; set; }
}