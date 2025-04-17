using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using System.Linq;
using HR_KD.DTOs;
using Microsoft.EntityFrameworkCore;

[Route("api/AttendanceRequestManager")]
[ApiController]
public class AttendanceRequestManagerApiController : ControllerBase
{
    private readonly HrDbContext _context;

    public AttendanceRequestManagerApiController(HrDbContext context)
    {
        _context = context;
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

    // API Chấm công
    [HttpPost]
    [Route("SubmitAttendanceRequest")]
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

                bool daChamCong = await _context.YeuCauSuaChamCongs.AnyAsync(c => c.MaNv == maNv.Value && c.NgayLamViec == ngayLamViec);
                if (daChamCong)
                {
                    return BadRequest(new { success = false, message = $"Nhân viên {maNv} đã có yêu cầu sửa chấm công ngày {entry.NgayLamViec}." });
                }
                bool daNghi = await _context.NgayNghis.AnyAsync(c => c.MaNv == maNv.Value && c.NgayNghi1 == ngayLamViec);
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
                    TrangThai = entry.TrangThai,
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

