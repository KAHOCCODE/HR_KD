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
   
    // 🔹 Gửi email nhắc nhở chấm công
    [HttpPost("SendReminderEmails")]
    public async Task<IActionResult> SendReminderEmails( SendReminderEmailsDTO request)
    {
        if (string.IsNullOrEmpty(request.StartDate) || !DateOnly.TryParse(request.StartDate, out var startDate))
        {
            return BadRequest(new { success = false, message = "Ngày bắt đầu không hợp lệ." });
        }

        try
        {
            // Calculate the week range
            var endDate = startDate.AddDays(6);
            var workingDays = new List<DateOnly>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.ToDateTime(TimeOnly.MinValue).DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays.Add(date);
                }
            }

            // Get all employees with email
            var employees = await _context.NhanViens
                .Where(nv => !string.IsNullOrEmpty(nv.Email))
                .Select(nv => new { nv.MaNv, nv.HoTen, nv.Email })
                .ToListAsync();

            // Get attendance records for the week from LichSuChamCong
            var attendanceRecords = await _context.LichSuChamCongs
                .Where(lsc => lsc.Ngay >= startDate && lsc.Ngay <= endDate)
                .Select(lsc => new { lsc.MaNv, lsc.Ngay })
                .ToListAsync();

            var employeesToRemind = new List<(int MaNv, string HoTen, string Email)>();
            foreach (var employee in employees)
            {
                foreach (var day in workingDays)
                {
                    if (!attendanceRecords.Any(ar => ar.MaNv == employee.MaNv && ar.Ngay == day))
                    {
                        employeesToRemind.Add((employee.MaNv, employee.HoTen, employee.Email));
                        break; // Only need one missing day to send reminder
                    }
                }
            }

            if (!employeesToRemind.Any())
            {
                return Ok(new { success = true, message = "Tất cả nhân viên đã chấm công đầy đủ trong tuần này." });
            }

            // Retrieve SMTP settings from configuration
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var port = int.Parse(_configuration["EmailSettings:Port"]);
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];

            if (string.IsNullOrEmpty(smtpServer) || port == 0 || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                return StatusCode(500, new { success = false, message = "Cấu hình email không hợp lệ." });
            }

            // Configure SMTP client
            using var smtpClient = new SmtpClient(smtpServer)
            {
                Port = port,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            foreach (var employee in employeesToRemind)
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = $"Nhắc nhở chấm công tuần từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}",
                    Body = $@"Kính gửi {employee.HoTen},

                    Đây là email nhắc nhở bạn vui lòng thực hiện chấm công cho các ngày làm việc trong tuần từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}.

                    Trân trọng,",
                    IsBodyHtml = false // Set to true if you want to send HTML email
                };
                mailMessage.To.Add(employee.Email);

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (SmtpException smtpEx)
                {
                    // Log the error, but continue sending to other employees
                    Console.WriteLine($"Lỗi gửi email đến {employee.Email}: {smtpEx.Message}");
                }
            }

            return Ok(new { success = true, message = $"Đã gửi email nhắc nhở đến {employeesToRemind.Count} nhân viên." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi khi gửi email nhắc nhở.",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    // 🔹 API Chấm công
    [HttpPost("SubmitAttendanceRequest")]
    public async Task<IActionResult> SubmitAttendanceRequest(List<YeuCauSuaChamCongDTO> attendanceData)
    {
        var maNv = GetMaNvFromClaims();
        if (!maNv.HasValue)
        {
            return Unauthorized(new { success = false, message = "Không xác định được nhân viên." });
        }

        if (attendanceData == null || !attendanceData.Any())
        {
            return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
        }

        try
        {
            foreach (var entry in attendanceData)
            {
                if (!DateOnly.TryParse(entry.NgayLamViec, out var ngayLamViec))
                {
                    return BadRequest(new { success = false, message = $"Ngày làm việc không hợp lệ: {entry.NgayLamViec}" });
                }

                bool daChamCong = await _context.YeuCauSuaChamCongs
                    .AnyAsync(c => c.MaNv == maNv.Value && c.NgayLamViec == ngayLamViec);
                if (daChamCong)
                {
                    return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã có yêu cầu sửa chấm công ngày {entry.NgayLamViec}." });
                }

                bool daNghi = await _context.NgayNghis
                    .AnyAsync(c => c.MaNv == maNv.Value && c.NgayNghi1 == ngayLamViec);
                if (daNghi)
                {
                    return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã nghỉ ngày {entry.NgayLamViec}.", error = "Employee on leave", stackTrace = "NgayNghi check failed." });
                }

                var yeuCauSuaChamCong = new YeuCauSuaChamCong
                {
                    MaNv = maNv.Value,
                    NgayLamViec = ngayLamViec,
                    GioVaoMoi = TimeOnly.TryParse(entry.GioVaoMoi, out var parsedGioVao) ? parsedGioVao : null,
                    GioRaMoi = TimeOnly.TryParse(entry.GioRaMoi, out var parsedGioRa) ? parsedGioRa : null,
                    TongGio = entry.TongGio ?? 0,
                    TrangThai = 0, // Set default status for new requests
                    LyDo = entry.LyDo
                };
                _context.YeuCauSuaChamCongs.Add(yeuCauSuaChamCong);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Yêu cầu sửa chấm công thành công." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = "Lỗi hệ thống.",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
    
}