using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using System.Linq;

[Route("api/AttendanceManager")]
[ApiController]
public class AttendanceManagerController : ControllerBase
{
    private readonly HrDbContext _context;

    public AttendanceManagerController(HrDbContext context)
    {
        _context = context;
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

    // 🔹 Duyệt hoặc từ chối chấm công
    [HttpPost("ApproveAttendanceManager")]
    public IActionResult ApproveAttendance([FromBody] ApproveAttendanceRequest request)
    {
        var chamCong = _context.ChamCongs.FirstOrDefault(cc => cc.MaChamCong == request.MaChamCong);
        if (chamCong == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy chấm công." });
        }

        chamCong.TrangThai = request.TrangThai;
        _context.SaveChanges();

        return Ok(new { success = true, message = "Cập nhật trạng thái thành công." });
    }
}

// DTO cho yêu cầu cập nhật trạng thái chấm công
public class ApproveAttendanceRequest
{
    public int MaChamCong { get; set; }
    public string TrangThai { get; set; } = "Chờ duyệt";
}
