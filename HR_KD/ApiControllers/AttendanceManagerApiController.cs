using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using System.Linq;
using HR_KD.DTOs;

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
        var records = _context.LichSuChamCongs
            .Where(cc => cc.MaNv == maNv)
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
            TrangThai = "Đã duyệt",
            GhiChu = lichSu.GhiChu
        };

        _context.ChamCongs.Add(chamCong);

        // Gọi hàm cập nhật trạng thái lịch sử
        CapNhatTrangThaiLichSuChamCong(request.MaChamCong, "Đã duyệt");

        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt chấm công thành công." });
    }
    private void CapNhatTrangThaiLichSuChamCong(int maLichSuChamCong, string trangThai)
    {
        var lichSu = _context.LichSuChamCongs.FirstOrDefault(cc => cc.MaLichSuChamCong == maLichSuChamCong);
        if (lichSu != null)
        {
            lichSu.TrangThai = trangThai;
        }
    }


    [HttpPost("ApproveAttendanceAndUpdateSalary")]
public IActionResult ApproveAttendanceAndUpdateSalary(ApproveAttendanceRequestDTO request)
{
    var chamCong = _context.ChamCongs.FirstOrDefault(cc => cc.MaChamCong == request.MaChamCong);
    if (chamCong == null)
    {
        return BadRequest(new { success = false, message = "Không tìm thấy chấm công." });
    }

    // Cập nhật trạng thái chấm công
    chamCong.TrangThai = request.TrangThai;

    if (request.TrangThai == "Đã duyệt")
    {
        // Kiểm tra dữ liệu cần thiết
        if (chamCong.TongGio == null || chamCong.NgayLamViec == null)
        {
            return BadRequest(new { success = false, message = "Dữ liệu chấm công không hợp lệ." });
        }

        const decimal BASE_SALARY_PER_HOUR = 100000m;
        decimal totalHours = (decimal)(chamCong.TongGio > 8 ? 8 : chamCong.TongGio);
        decimal overtimeHours = (decimal)(chamCong.TongGio > 8 ? chamCong.TongGio - 8 : 0);

        decimal baseSalary = totalHours * BASE_SALARY_PER_HOUR;
        decimal overtimeSalary = 0;

        if (overtimeHours > 0)
        {
            overtimeSalary = overtimeHours * BASE_SALARY_PER_HOUR * 2;
        }

        decimal grossSalary = baseSalary + overtimeSalary;
        decimal tax = grossSalary * 0.1m;
        decimal netSalary = grossSalary - tax;

        // Thêm bản ghi bảng lương
        var salaryRecord = new BangLuong
        {
            MaNv = chamCong.MaNv,
            
            ThangNam = chamCong.NgayLamViec,
            PhuCapThem = 0,
            LuongThem = 0,
            LuongTangCa = overtimeSalary,
            ThueTNCN = tax,
            TongLuong = grossSalary,
            ThucNhan = netSalary,
            TrangThai = "Chưa thanh toán",
            GhiChu = "Tự động tạo từ duyệt chấm công"
        };

        _context.BangLuongs.Add(salaryRecord);
    }

    _context.SaveChanges();

    return Ok(new
    {
        success = true,
        message = "Cập nhật trạng thái và bảng lương thành công.",
        data = new
        {
            chamCong.MaChamCong
        }
    });
}



    [HttpGet("GetEmployeeSalaries")]
    public IActionResult GetEmployeeSalaries(int maNv)
    {
        var salaries = _context.BangLuongs
            .Where(bl => bl.MaNv == maNv)
            .Select(bl => new
            {
                bl.ThangNam,
                bl.LuongTangCa,
                bl.ThueTNCN,
                bl.TongLuong,
                bl.ThucNhan,
                bl.TrangThai,
                bl.GhiChu
            })
            .ToList();

        return Ok(new { success = true, records = salaries });
    }

}


