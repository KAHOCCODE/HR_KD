using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using System.Linq;
using HR_KD.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

[Route("api/AttendanceManager")]
[ApiController]
public class AttendanceManagerController : ControllerBase
{
    private readonly HrDbContext _context;
    private readonly IConfiguration _configuration; // Add this

    public AttendanceManagerController(HrDbContext context, IConfiguration configuration) // Update constructor
    {
        _context = context;
        _configuration = configuration;
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
        var records = _context.LichSuChamCongs
            .Where(cc => cc.MaNv == maNv && (cc.TrangThai == null || cc.TrangThai == "Chờ duyệt"))
            .Select(cc => new
            {
                cc.MaLichSuChamCong,
                cc.Ngay,
                cc.GioVao,
                cc.GioRa,
                cc.TongGio,
                TrangThai = cc.TrangThai ?? "Chờ duyệt",
                cc.GhiChu
            })
            .ToList();

        return Ok(new { success = true, records });
    }

    // 🔹 Duyệt hoặc từ chối chấm công
    [HttpPost("ApproveAttendanceManager")]
    public IActionResult ApproveAttendance(ApproveAttendanceRequestDTO request)
    {
        // Tìm bản ghi trong lịch sử chấm công
        var lichSu = _context.LichSuChamCongs.FirstOrDefault(cc => cc.MaLichSuChamCong == request.MaChamCong);
        if (lichSu == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy lịch sử chấm công." });
        }

        // Nếu từ chối thì chỉ cập nhật trạng thái
        if (request.TrangThai == "Từ chối")
        {
            CapNhatTrangThaiLichSuChamCong(request.MaChamCong, "Từ chối");
            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối chấm công." });
        }

        // Nếu duyệt thì chuyển sang bảng ChamCong
        var daTonTai = _context.ChamCongs.Any(cc => cc.MaNv == lichSu.MaNv && cc.NgayLamViec == lichSu.Ngay);
        if (daTonTai)
        {
            return BadRequest(new { success = false, message = "Chấm công đã tồn tại trong bảng chính." });
        }

        var chamCong = new ChamCong
        {
            MaNv = lichSu.MaNv,
            NgayLamViec = lichSu.Ngay,
            GioVao = lichSu.GioVao,
            GioRa = lichSu.GioRa,
            TongGio = lichSu.TongGio,
            TrangThai = "Đã duyệt lận 1",
            GhiChu = lichSu.GhiChu
        };

        _context.ChamCongs.Add(chamCong);
        //var employee = _context.NhanViens.Find(chamCong.MaNv);
        //if (employee != null)
        //{
        //    SendApprovalEmail(employee.Email, employee.HoTen, chamCong.NgayLamViec, request.TrangThai);
        //}
        // Gọi hàm cập nhật trạng thái lịch sử
        CapNhatTrangThaiLichSuChamCong(request.MaChamCong, "Đã duyệt");

        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt chấm công thành công." });
    }
    private void SendApprovalEmail(string recipientEmail, string employeeName, DateOnly ngay, string trangThai)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"];
        var senderPassword = emailSettings["SenderPassword"];
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"]);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("HR Department", senderEmail));
        message.To.Add(new MailboxAddress(employeeName, recipientEmail));
        message.Subject = "Thông báo kết quả duyệt";

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $"<p>Kính gửi {employeeName},</p>" +
                         $"<p>Thông tin ngày {ngay.ToString("dd/MM/yyyy")} của bạn đã được duyệt.</p>" +  // Or your preferred format
                         $"<p>Trạng thái: <b>{trangThai}</b></p>" +
                         $"<p>Vui lòng kiểm tra lại thông tin trên hệ thống.</p>" +
                         $"<p>Trân trọng,</p>" +
                         $"<p>Phòng Nhân sự</p>";

        message.Body = bodyBuilder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            client.Connect(smtpServer, port, SecureSocketOptions.StartTls);
            client.Authenticate(senderEmail, senderPassword);
            client.Send(message);
            client.Disconnect(true);
        }
    }
    private void CapNhatTrangThaiLichSuChamCong(int maLichSuChamCong, string trangThai)
    {
        var lichSu = _context.LichSuChamCongs.FirstOrDefault(cc => cc.MaLichSuChamCong == maLichSuChamCong);
        if (lichSu != null)
        {
            lichSu.TrangThai = trangThai;
        }
    }




    // lấy danh sách tăng ca của nhân viên
    [HttpGet("GetOvertimeRecords")]
    public IActionResult GetOvertimeRecords(int maNv)
    {
        var overtimeRecords = _context.TangCas
            .Where(tc => tc.MaNv == maNv && (tc.TrangThai == null || tc.TrangThai == "Chờ duyệt"))
            .Select(tc => new
            {
                tc.MaTangCa,
                tc.NgayTangCa,
                tc.SoGioTangCa,
                tc.TyLeTangCa,
                TrangThai = tc.TrangThai ?? "Chờ duyệt"
            })
            .ToList();

        return Ok(new { success = true, records = overtimeRecords });
    }
    // duyệt hoặc từ chối tăng ca
    [HttpPost("ApproveOvertime")]
    public IActionResult ApproveOvertime(ApproveAttendanceRequestDTO request)
    {
        // Tìm bản ghi trong bảng TangCa
        var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == request.MaChamCong);
        if (tangCa == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy yêu cầu tăng ca." });
        }

        // Nếu từ chối thì chỉ cập nhật trạng thái
        if (request.TrangThai == "Từ chối")
        {
            CapNhatTrangThaiTangCa(request.MaChamCong, "Từ chối");
            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối yêu cầu tăng ca." });
        }

        // Nếu duyệt thì chuyển sang bảng ChamCong
        var daTonTai = _context.ChamCongs.Any(cc => cc.MaNv == tangCa.MaNv && cc.NgayLamViec == tangCa.NgayTangCa);
        if (daTonTai)
        {
            return BadRequest(new { success = false, message = "Chấm công đã tồn tại trong bảng chính." });
        }

        var chamCong = new ChamCong
        {
            MaNv = tangCa.MaNv,
            NgayLamViec = tangCa.NgayTangCa,
            GioVao = null,
            GioRa = null,
            TongGio = tangCa.SoGioTangCa,
            TrangThai = "Đã duyệt lận 1",
            GhiChu = "Tăng ca"
        };

        _context.ChamCongs.Add(chamCong);

        // Gọi hàm cập nhật trạng thái lịch sử
        CapNhatTrangThaiTangCa(request.MaChamCong, "Đã duyệt lận 1");

        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt yêu cầu tăng ca thành công." });
    }
    private void CapNhatTrangThaiTangCa(int maTangCa, string trangThai)
    {
        var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == maTangCa);
        if (tangCa != null)
        {
            tangCa.TrangThai = trangThai;
        }
    }
    //API gửi view directior
    [HttpGet("GetOvertimeRecordsDerector")]
    public IActionResult GetOvertimeRecordsDerector(int maNv)
    {
        var overtimeRecords = _context.TangCas
            .Where(tc => tc.MaNv == maNv && (tc.TrangThai == null || tc.TrangThai == "Đã duyệt lận 1"))
            .Select(tc => new
            {
                tc.MaTangCa,
                tc.NgayTangCa,
                tc.SoGioTangCa,
                tc.TyLeTangCa,
                TrangThai = tc.TrangThai ?? "Đã duyệt lận 1"
            })
            .ToList();

        return Ok(new { success = true, records = overtimeRecords });
    }
    // duyệt hoặc từ chối tăng ca
    [HttpPost("ApproveOvertimeDerector")]
    public IActionResult ApproveOvertimeDerector(ApproveAttendanceRequestDTO request)
    {
        // Tìm bản ghi trong bảng TangCa
        var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == request.MaChamCong);
        if (tangCa == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy yêu cầu tăng ca." });
        }

        // Nếu từ chối thì chỉ cập nhật trạng thái
        if (request.TrangThai == "Từ chối")
        {
            CapNhatTrangThaiTangCa(request.MaChamCong, "Từ chối");
            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối yêu cầu tăng ca." });
        }

        // Nếu duyệt thì chuyển sang bảng ChamCong
        var daTonTai = _context.ChamCongs.Any(cc => cc.MaNv == tangCa.MaNv && cc.NgayLamViec == tangCa.NgayTangCa);
        if (daTonTai)
        {
            return BadRequest(new { success = false, message = "Chấm công đã tồn tại trong bảng chính." });
        }

        var chamCong = new ChamCong
        {
            MaNv = tangCa.MaNv,
            NgayLamViec = tangCa.NgayTangCa,
            GioVao = null,
            GioRa = null,
            TongGio = tangCa.SoGioTangCa,
            TrangThai = "Đã duyệt",
            GhiChu = "Tăng ca"
        };

        _context.ChamCongs.Add(chamCong);

        // Gọi hàm cập nhật trạng thái lịch sử
        CapNhatTrangThaiTangCa(request.MaChamCong, "Đã duyệt");

        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt yêu cầu tăng ca thành công." });
    }
    // 🔹 Lấy danh sách chấm công của nhân viên
    [HttpGet("GetAttendanceManagerRecordsDerector")]
    public IActionResult GetAttendanceRecordsDerector(int maNv)
    {
        var records = _context.ChamCongs
            .Where(cc => cc.MaNv == maNv && (cc.TrangThai == null || cc.TrangThai == "Đã duyệt lận 1"))
            .Select(cc => new
            {
                cc.MaChamCong,
                cc.NgayLamViec,
                cc.GioVao,
                cc.GioRa,
                cc.TongGio,
                TrangThai = cc.TrangThai ?? "Đã duyệt lận 1",
                cc.GhiChu
            })
            .ToList();

        return Ok(new { success = true, records });
    }

    // 🔹 Duyệt hoặc từ chối chấm công
    [HttpPost("ApproveAttendanceManagerDerector")]
    public IActionResult ApproveAttendanceDerector(ApproveAttendanceRequestDTO request)
    {
        // Tìm bản ghi trong lịch sử chấm công
        var lichSu = _context.LichSuChamCongs.FirstOrDefault(cc => cc.MaLichSuChamCong == request.MaChamCong);
        if (lichSu == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy lịch sử chấm công." });
        }

        // Nếu từ chối thì chỉ cập nhật trạng thái
        if (request.TrangThai == "Từ chối")
        {
            CapNhatTrangThaiLichSuChamCong(request.MaChamCong, "Từ chối");
            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối chấm công." });
        }

        // Nếu duyệt thì chuyển sang bảng ChamCong
        var daTonTai = _context.ChamCongs.Any(cc => cc.MaNv == lichSu.MaNv && cc.NgayLamViec == lichSu.Ngay);
        if (daTonTai)
        {
            return BadRequest(new { success = false, message = "Chấm công đã tồn tại trong bảng chính." });
        }

        var chamCong = new ChamCong
        {
            MaNv = lichSu.MaNv,
            NgayLamViec = lichSu.Ngay,
            GioVao = lichSu.GioVao,
            GioRa = lichSu.GioRa,
            TongGio = lichSu.TongGio,
            TrangThai = "Đã duyệt",
            GhiChu = lichSu.GhiChu
        };

        _context.ChamCongs.Add(chamCong);
        var employee = _context.NhanViens.Find(chamCong.MaNv);
        if (employee != null)
        {
            SendApprovalEmail(employee.Email, employee.HoTen, chamCong.NgayLamViec, request.TrangThai);
        }
        // Gọi hàm cập nhật trạng thái lịch sử
        CapNhatTrangThaiLichSuChamCong(request.MaChamCong, "Đã duyệt");

        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt chấm công thành công." });
    }
}


