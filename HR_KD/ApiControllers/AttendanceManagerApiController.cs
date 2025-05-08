using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using System.Linq;
using HR_KD.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

[Route("api/AttendanceManager")]
[ApiController]
public class AttendanceManagerController : ControllerBase
{
    private readonly HrDbContext _context;
    private readonly IConfiguration _configuration;

    public AttendanceManagerController(HrDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Lấy MaNv từ claims
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

    [HttpGet("GetDepartmentsManagerIndex")]
    public IActionResult GetDepartmentsIndex()
    {
        int? maNv = GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var departments = _context.NhanViens
            .Where(nv => nv.MaNv == maNv)
            .Select(nv => new
            {
                nv.MaPhongBanNavigation.MaPhongBan,
                nv.MaPhongBanNavigation.TenPhongBan
            })
            .FirstOrDefault();

        if (departments == null)
            return NotFound("Không tìm thấy phòng ban của nhân viên.");

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

    // 🔹 Lấy danh sách trạng thái
    [HttpGet("GetTrangThai")]
    public IActionResult GetTrangThai()
    {
        var trangThai = _context.TrangThais
            .Select(tt => new { tt.MaTrangThai, tt.TenTrangThai })
            .ToList();

        return Ok(trangThai);
    }

    // 🔹 Lấy danh sách chấm công của nhân viên (Manager)
    [HttpGet("GetAttendanceManagerRecords")]
    public IActionResult GetAttendanceRecords(int maNv)
    {
        var records = _context.LichSuChamCongs
            .Where(cc => cc.MaNv == maNv && (cc.TrangThai == null || cc.TrangThai == "LS1"))
            .Select(cc => new
            {
                cc.MaLichSuChamCong,
                cc.Ngay,
                cc.GioVao,
                cc.GioRa,
                cc.TongGio,
                TrangThai = cc.TrangThai ?? "LS1",
                cc.GhiChu,
                cc.MaNvDuyet
            })
            .ToList();

        return Ok(new { success = true, records });
    }

    // 🔹 Duyệt hoặc từ chối chấm công (Manager, đơn lẻ)
    [HttpPost("ApproveAttendanceManager")]
    public IActionResult ApproveAttendance(ApproveAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var lichSu = _context.LichSuChamCongs.FirstOrDefault(cc => cc.MaLichSuChamCong == request.MaChamCong);
        if (lichSu == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy lịch sử chấm công." });
        }

        if (request.TrangThai == "LS4")
        {
            lichSu.TrangThai = "LS4";
            lichSu.GhiChu = request.GhiChu ?? "Không có ghi chú";
            lichSu.MaNvDuyet = maNv;

            var employee = _context.NhanViens.Find(lichSu.MaNv);
            if (employee != null)
            {
                SendRejectionEmail(employee.Email, employee.HoTen, lichSu.Ngay, "LS4", "chấm công", lichSu.GhiChu);
            }

            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối chấm công." });
        }

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
            TrangThai = "CC2",
            GhiChu = lichSu.GhiChu,
            MaNvDuyet = maNv
        };

        _context.ChamCongs.Add(chamCong);
        lichSu.TrangThai = "LS2";
        lichSu.MaNvDuyet = maNv;
        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt chấm công thành công." });
    }

    // 🔹 Duyệt hoặc từ chối nhiều bản ghi chấm công (Manager)
    [HttpPost("ApproveMultipleAttendanceManager")]
    public IActionResult ApproveMultipleAttendance([FromBody] ApproveMultipleAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var failedRecords = new List<int>();
                var rejectionDetails = new List<(string Email, string HoTen, DateOnly Ngay, string GhiChu)>();

                foreach (var maChamCong in request.MaChamCongList)
                {
                    var lichSu = _context.LichSuChamCongs.FirstOrDefault(cc => cc.MaLichSuChamCong == maChamCong);
                    if (lichSu == null)
                    {
                        failedRecords.Add(maChamCong);
                        continue;
                    }

                    if (lichSu.TrangThai != "LS1" && lichSu.TrangThai != null)
                    {
                        failedRecords.Add(maChamCong);
                        continue;
                    }

                    if (request.TrangThai == "LS4")
                    {
                        lichSu.TrangThai = "LS4";
                        lichSu.GhiChu = request.GhiChu ?? "Không có ghi chú";
                        lichSu.MaNvDuyet = maNv;

                        var employee = _context.NhanViens.Find(lichSu.MaNv);
                        if (employee != null)
                        {
                            rejectionDetails.Add((employee.Email, employee.HoTen, lichSu.Ngay, lichSu.GhiChu));
                        }
                    }
                    else if (request.TrangThai == "LS2")
                    {
                        var daTonTai = _context.ChamCongs.Any(cc => cc.MaNv == lichSu.MaNv && cc.NgayLamViec == lichSu.Ngay);
                        if (daTonTai)
                        {
                            failedRecords.Add(maChamCong);
                            continue;
                        }

                        var chamCong = new ChamCong
                        {
                            MaNv = lichSu.MaNv,
                            NgayLamViec = lichSu.Ngay,
                            GioVao = lichSu.GioVao,
                            GioRa = lichSu.GioRa,
                            TongGio = lichSu.TongGio,
                            TrangThai = "CC2",
                            GhiChu = lichSu.GhiChu,
                            MaNvDuyet = maNv
                        };

                        _context.ChamCongs.Add(chamCong);
                        lichSu.TrangThai = "LS2";
                        lichSu.MaNvDuyet = maNv;
                    }
                }

                _context.SaveChanges();

                if (request.TrangThai == "LS4" && rejectionDetails.Any())
                {
                    foreach (var group in rejectionDetails.GroupBy(d => d.Email))
                    {
                        var email = group.Key;
                        var hoTen = group.First().HoTen;
                        var details = group.Select(d => $"Ngày {d.Ngay:dd/MM/yyyy}: {d.GhiChu}").ToList();
                        SendBatchRejectionEmail(email, hoTen, "chấm công", details);
                    }
                }

                transaction.Commit();
                var baseMessage = request.TrangThai == "LS2" ? "Duyệt chấm công thành công." : "Đã từ chối chấm công.";
                var message = failedRecords.Any()
                    ? $"{baseMessage} Tuy nhiên, các bản ghi {string.Join(", ", failedRecords)} không được cập nhật."
                    : baseMessage;
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // 🔹 Lấy danh sách tăng ca của nhân viên (Manager)
    [HttpGet("GetOvertimeRecords")]
    public IActionResult GetOvertimeRecords(int maNv)
    {
        var overtimeRecords = _context.TangCas
            .Where(tc => tc.MaNv == maNv && (tc.TrangThai == null || tc.TrangThai == "TC1"))
            .Select(tc => new
            {
                tc.MaTangCa,
                tc.NgayTangCa,
                tc.SoGioTangCa,
                tc.TyLeTangCa,
                TrangThai = tc.TrangThai ?? "TC1",
                tc.MaNvDuyet
            })
            .ToList();

        return Ok(new { success = true, records = overtimeRecords });
    }

    // 🔹 Duyệt hoặc từ chối tăng ca (Manager, đơn lẻ)
    [HttpPost("ApproveOvertime")]
    public IActionResult ApproveOvertime(ApproveAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == request.MaChamCong);
        if (tangCa == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy yêu cầu tăng ca." });
        }

        // Kiểm tra giờ thiếu còn lại
        var firstDayOfMonth = new DateOnly(tangCa.NgayTangCa.Year, tangCa.NgayTangCa.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        var tongGioThieu = _context.TongGioThieus
            .FirstOrDefault(t => t.MaNv == tangCa.MaNv &&
                               t.NgayBatDauThieu == firstDayOfMonth &&
                               t.NgayKetThucThieu == lastDayOfMonth);

        if (tangCa.TrangThai == "TC4")
        {
            tangCa.TrangThai = "TC4";
            tangCa.GhiChu = request.GhiChu ?? "Không có ghi chú";
            tangCa.MaNvDuyet = maNv;

            var employee = _context.NhanViens.Find(tangCa.MaNv);
            if (employee != null)
            {
                SendRejectionEmail(employee.Email, employee.HoTen, tangCa.NgayTangCa, "TC4", "tăng ca", tangCa.GhiChu);
            }

            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối tăng ca." });
        }

        // Kiểm tra nếu còn giờ thiếu
        if (tongGioThieu != null && (tongGioThieu.TongGioConThieu - tongGioThieu.TongGioLamBu) > 0)
        {
            return BadRequest(new { success = false, message = "Không thể duyệt tăng ca vì nhân viên còn giờ thiếu chưa được bù." });
        }

        tangCa.TrangThai = "TC2";
        tangCa.MaNvDuyet = maNv;
        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt tăng ca thành công." });
    }

    // 🔹 Duyệt hoặc từ chối nhiều bản ghi tăng ca (Manager)
    [HttpPost("ApproveMultipleOvertime")]
    public IActionResult ApproveMultipleOvertime([FromBody] ApproveMultipleAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var failedRecords = new List<int>();
                var rejectionDetails = new List<(string Email, string HoTen, DateOnly Ngay, string GhiChu)>();

                foreach (var maTangCa in request.MaChamCongList)
                {
                    var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == maTangCa);
                    if (tangCa == null)
                    {
                        failedRecords.Add(maTangCa);
                        continue;
                    }

                    if (tangCa.TrangThai != "TC1" && tangCa.TrangThai != null)
                    {
                        failedRecords.Add(maTangCa);
                        continue;
                    }

                    // Kiểm tra giờ thiếu còn lại
                    var firstDayOfMonth = new DateOnly(tangCa.NgayTangCa.Year, tangCa.NgayTangCa.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    var tongGioThieu = _context.TongGioThieus
                        .FirstOrDefault(t => t.MaNv == tangCa.MaNv &&
                                           t.NgayBatDauThieu == firstDayOfMonth &&
                                           t.NgayKetThucThieu == lastDayOfMonth);

                    if (request.TrangThai == "TC4")
                    {
                        tangCa.TrangThai = "TC4";
                        tangCa.GhiChu = request.GhiChu ?? "Không có ghi chú";
                        tangCa.MaNvDuyet = maNv;

                        var employee = _context.NhanViens.Find(tangCa.MaNv);
                        if (employee != null)
                        {
                            rejectionDetails.Add((employee.Email, employee.HoTen, tangCa.NgayTangCa, tangCa.GhiChu));
                        }
                    }
                    else if (request.TrangThai == "TC2")
                    {
                        // Kiểm tra nếu còn giờ thiếu
                        if (tongGioThieu != null && (tongGioThieu.TongGioConThieu - tongGioThieu.TongGioLamBu) > 0)
                        {
                            failedRecords.Add(maTangCa);
                            continue;
                        }

                        tangCa.TrangThai = "TC2";
                        tangCa.MaNvDuyet = maNv;
                    }
                }

                _context.SaveChanges();

                if (request.TrangThai == "TC4" && rejectionDetails.Any())
                {
                    foreach (var group in rejectionDetails.GroupBy(d => d.Email))
                    {
                        var email = group.Key;
                        var hoTen = group.First().HoTen;
                        var details = group.Select(d => $"Ngày {d.Ngay:dd/MM/yyyy}: {d.GhiChu}").ToList();
                        SendBatchRejectionEmail(email, hoTen, "tăng ca", details);
                    }
                }

                transaction.Commit();
                var baseMessage = request.TrangThai == "TC2" ? "Duyệt tăng ca thành công." : "Đã từ chối tăng ca.";
                var message = failedRecords.Any()
                    ? $"{baseMessage} Tuy nhiên, các bản ghi {string.Join(", ", failedRecords)} không được cập nhật do nhân viên còn giờ thiếu chưa được bù."
                    : baseMessage;
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // 🔹 Lấy danh sách chấm công của nhân viên (Director)
    [HttpGet("GetAttendanceManagerRecordsDerector")]
    public IActionResult GetAttendanceRecordsDerector(int maNv)
    {
        var records = _context.ChamCongs
            .Where(cc => cc.MaNv == maNv && (cc.TrangThai == null || cc.TrangThai == "CC2"))
            .Select(cc => new
            {
                cc.MaChamCong,
                cc.NgayLamViec,
                cc.GioVao,
                cc.GioRa,
                cc.TongGio,
                TrangThai = cc.TrangThai ?? "CC2",
                cc.GhiChu,
                cc.MaNvDuyet
            })
            .ToList();

        return Ok(new { success = true, records });
    }

    // 🔹 Duyệt hoặc từ chối chấm công (Director, đơn lẻ)
    [HttpPost("ApproveAttendanceManagerDerector")]
    public IActionResult ApproveAttendanceDerector(ApproveAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var chamCong = _context.ChamCongs.FirstOrDefault(cc => cc.MaChamCong == request.MaChamCong);
        if (chamCong == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy lịch sử chấm công." });
        }

        var employee = _context.NhanViens.Find(chamCong.MaNv);
        if (employee == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy nhân viên." });
        }

        if (request.TrangThai == "Đã duyệt")
        {
            chamCong.TrangThai = "CC3";
            chamCong.MaNvDuyet = maNv;

            const decimal standardHours = 8.0m;
            if (chamCong.TongGio.HasValue && chamCong.TongGio < standardHours)
            {
                decimal shortfall = standardHours - chamCong.TongGio.Value;

                var gioThieu = new GioThieu
                {
                    NgayThieu = chamCong.NgayLamViec,
                    TongGioThieu = shortfall,
                    MaNv = chamCong.MaNv,
                    MaNvNavigation = employee
                };
                _context.GioThieus.Add(gioThieu);

                UpdateTongGioThieu(chamCong.MaNv, chamCong.NgayLamViec, shortfall);
            }

            SendApprovalEmail(employee.Email, employee.HoTen, chamCong.NgayLamViec, "CC3");
        }
        else if (request.TrangThai == "Từ chối")
        {
            chamCong.TrangThai = "CC4";
            chamCong.GhiChu = request.GhiChu ?? "Không có ghi chú";
            chamCong.MaNvDuyet = maNv;
            SendRejectionEmail(employee.Email, employee.HoTen, chamCong.NgayLamViec, "CC4", "chấm công", chamCong.GhiChu);
        }

        _context.SaveChanges();
        var message = request.TrangThai == "Đã duyệt" ? "Duyệt chấm công thành công." : "Đã từ chối chấm công.";
        return Ok(new { success = true, message });
    }

    // 🔹 Duyệt hoặc từ chối nhiều bản ghi chấm công (Director)
    [HttpPost("ApproveMultipleAttendanceManagerDerector")]
    public IActionResult ApproveMultipleAttendanceDerector([FromBody] ApproveMultipleAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var failedRecords = new List<int>();
                var rejectionDetails = new List<(string Email, string HoTen, DateOnly Ngay, string GhiChu)>();
                const decimal standardHours = 8.0m;

                foreach (var maChamCong in request.MaChamCongList)
                {
                    var chamCong = _context.ChamCongs.FirstOrDefault(cc => cc.MaChamCong == maChamCong);
                    if (chamCong == null)
                    {
                        failedRecords.Add(maChamCong);
                        continue;
                    }

                    if (chamCong.TrangThai != "CC2" && chamCong.TrangThai != null)
                    {
                        failedRecords.Add(maChamCong);
                        continue;
                    }

                    var employee = _context.NhanViens.Find(chamCong.MaNv);
                    if (employee == null)
                    {
                        failedRecords.Add(maChamCong);
                        continue;
                    }

                    if (request.TrangThai == "Từ chối")
                    {
                        chamCong.TrangThai = "CC4";
                        chamCong.GhiChu = request.GhiChu ?? "Không có ghi chú";
                        chamCong.MaNvDuyet = maNv;
                        rejectionDetails.Add((employee.Email, employee.HoTen, chamCong.NgayLamViec, chamCong.GhiChu));
                    }
                    else if (request.TrangThai == "Đã duyệt")
                    {
                        chamCong.TrangThai = "CC3";
                        chamCong.MaNvDuyet = maNv;

                        if (chamCong.TongGio.HasValue && chamCong.TongGio < standardHours)
                        {
                            decimal shortfall = standardHours - chamCong.TongGio.Value;

                            var gioThieu = new GioThieu
                            {
                                NgayThieu = chamCong.NgayLamViec,
                                TongGioThieu = shortfall,
                                MaNv = chamCong.MaNv,
                                MaNvNavigation = employee
                            };
                            _context.GioThieus.Add(gioThieu);

                            UpdateTongGioThieu(chamCong.MaNv, chamCong.NgayLamViec, shortfall);
                        }

                        SendApprovalEmail(employee.Email, employee.HoTen, chamCong.NgayLamViec, "CC3");
                    }
                }

                _context.SaveChanges();

                if (request.TrangThai == "Từ chối" && rejectionDetails.Any())
                {
                    foreach (var group in rejectionDetails.GroupBy(d => d.Email))
                    {
                        var email = group.Key;
                        var hoTen = group.First().HoTen;
                        var details = group.Select(d => $"Ngày {d.Ngay:dd/MM/yyyy}: {d.GhiChu}").ToList();
                        SendBatchRejectionEmail(email, hoTen, "chấm công", details);
                    }
                }

                transaction.Commit();
                var baseMessage = request.TrangThai == "Đã duyệt" ? "Duyệt chấm công thành công." : "Đã từ chối chấm công.";
                var message = failedRecords.Any()
                    ? $"{baseMessage} Tuy nhiên, các bản ghi {string.Join(", ", failedRecords)} không được cập nhật."
                    : baseMessage;
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // 🔹 Lấy danh sách tăng ca của nhân viên (Director)
    [HttpGet("GetOvertimeRecordsDerector")]
    public IActionResult GetOvertimeRecordsDerector(int maNv)
    {
        var overtimeRecords = _context.TangCas
            .Where(tc => tc.MaNv == maNv && (tc.TrangThai == null || tc.TrangThai == "TC2"))
            .Select(tc => new
            {
                tc.MaTangCa,
                tc.NgayTangCa,
                tc.SoGioTangCa,
                tc.TyLeTangCa,
                TrangThai = tc.TrangThai ?? "TC2",
                tc.MaNvDuyet
            })
            .ToList();

        return Ok(new { success = true, records = overtimeRecords });
    }

    // 🔹 Duyệt hoặc từ chối tăng ca (Director, đơn lẻ)
    [HttpPost("ApproveOvertimeDerector")]
    public IActionResult ApproveOvertimeDerector(ApproveAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == request.MaChamCong);
        if (tangCa == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy yêu cầu tăng ca." });
        }

        // Kiểm tra giờ thiếu còn lại
        var firstDayOfMonth = new DateOnly(tangCa.NgayTangCa.Year, tangCa.NgayTangCa.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        var tongGioThieu = _context.TongGioThieus
            .FirstOrDefault(t => t.MaNv == tangCa.MaNv &&
                               t.NgayBatDauThieu == firstDayOfMonth &&
                               t.NgayKetThucThieu == lastDayOfMonth);

        tangCa.TrangThai = request.TrangThai == "Đã duyệt" ? "TC3" : "TC4";
        tangCa.GhiChu = request.TrangThai == "Từ chối" ? (request.GhiChu ?? "Không có ghi chú") : null;
        tangCa.MaNvDuyet = maNv;

        var employee = _context.NhanViens.Find(tangCa.MaNv);
        if (employee != null)
        {
            if (request.TrangThai == "Từ chối")
            {
                SendRejectionEmail(employee.Email, employee.HoTen, tangCa.NgayTangCa, "TC4", "tăng ca", tangCa.GhiChu);
            }
            else if (request.TrangThai == "Đã duyệt")
            {
                // Kiểm tra nếu còn giờ thiếu
                if (tongGioThieu != null && (tongGioThieu.TongGioConThieu - tongGioThieu.TongGioLamBu) > 0)
                {
                    return BadRequest(new { success = false, message = "Không thể duyệt tăng ca vì nhân viên còn giờ thiếu chưa được bù." });
                }
                SendApprovalEmail(employee.Email, employee.HoTen, tangCa.NgayTangCa, "TC3");
            }
        }

        _context.SaveChanges();
        var message = request.TrangThai == "Đã duyệt" ? "Duyệt tăng ca thành công." : "Đã từ chối tăng ca.";
        return Ok(new { success = true, message });
    }

    // 🔹 Duyệt hoặc từ chối nhiều bản ghi tăng ca (Director)
    [HttpPost("ApproveMultipleOvertimeDerector")]
    public IActionResult ApproveMultipleOvertimeDerector([FromBody] ApproveMultipleAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var failedRecords = new List<int>();
                var rejectionDetails = new List<(string Email, string HoTen, DateOnly Ngay, string GhiChu)>();

                foreach (var maTangCa in request.MaChamCongList)
                {
                    var tangCa = _context.TangCas.FirstOrDefault(tc => tc.MaTangCa == maTangCa);
                    if (tangCa == null)
                    {
                        failedRecords.Add(maTangCa);
                        continue;
                    }

                    if (tangCa.TrangThai != "TC2" && tangCa.TrangThai != null)
                    {
                        failedRecords.Add(maTangCa);
                        continue;
                    }

                    // Kiểm tra giờ thiếu còn lại
                    var firstDayOfMonth = new DateOnly(tangCa.NgayTangCa.Year, tangCa.NgayTangCa.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    var tongGioThieu = _context.TongGioThieus
                        .FirstOrDefault(t => t.MaNv == tangCa.MaNv &&
                                           t.NgayBatDauThieu == firstDayOfMonth &&
                                           t.NgayKetThucThieu == lastDayOfMonth);

                    if (request.TrangThai == "Từ chối")
                    {
                        tangCa.TrangThai = "TC4";
                        tangCa.GhiChu = request.GhiChu ?? "Không có ghi chú";
                        tangCa.MaNvDuyet = maNv;

                        var employee = _context.NhanViens.Find(tangCa.MaNv);
                        if (employee != null)
                        {
                            rejectionDetails.Add((employee.Email, employee.HoTen, tangCa.NgayTangCa, tangCa.GhiChu));
                        }
                    }
                    else if (request.TrangThai == "Đã duyệt")
                    {
                        // Kiểm tra nếu còn giờ thiếu
                        if (tongGioThieu != null && (tongGioThieu.TongGioConThieu - tongGioThieu.TongGioLamBu) > 0)
                        {
                            failedRecords.Add(maTangCa);
                            continue;
                        }

                        tangCa.TrangThai = "TC3";
                        tangCa.MaNvDuyet = maNv;

                        var employee = _context.NhanViens.Find(tangCa.MaNv);
                        if (employee != null)
                        {
                            SendApprovalEmail(employee.Email, employee.HoTen, tangCa.NgayTangCa, "TC3");
                        }
                    }
                }

                _context.SaveChanges();

                if (request.TrangThai == "Từ chối" && rejectionDetails.Any())
                {
                    foreach (var group in rejectionDetails.GroupBy(d => d.Email))
                    {
                        var email = group.Key;
                        var hoTen = group.First().HoTen;
                        var details = group.Select(d => $"Ngày {d.Ngay:dd/MM/yyyy}: {d.GhiChu}").ToList();
                        SendBatchRejectionEmail(email, hoTen, "tăng ca", details);
                    }
                }

                transaction.Commit();
                var baseMessage = request.TrangThai == "Đã duyệt" ? "Duyệt tăng ca thành công." : "Đã từ chối tăng ca.";
                var message = failedRecords.Any()
                    ? $"{baseMessage} Tuy nhiên, các bản ghi {string.Join(", ", failedRecords)} không được cập nhật do nhân viên còn giờ thiếu chưa được bù."
                    : baseMessage;
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // 🔹 Helper method to update or create TongGioThieu for the month
    private void UpdateTongGioThieu(int maNv, DateOnly ngayLamViec, decimal shortfall)
    {
        var firstDayOfMonth = new DateOnly(ngayLamViec.Year, ngayLamViec.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        var tongGioThieu = _context.TongGioThieus
            .FirstOrDefault(t => t.MaNv == maNv &&
                                 t.NgayBatDauThieu == firstDayOfMonth &&
                                 t.NgayKetThucThieu == lastDayOfMonth);

        if (tongGioThieu == null)
        {
            tongGioThieu = new TongGioThieu
            {
                MaNv = maNv,
                NgayBatDauThieu = firstDayOfMonth,
                NgayKetThucThieu = lastDayOfMonth,
                TongGioConThieu = shortfall,
                TongGioLamBu = 0m,
                MaNvNavigation = _context.NhanViens.Find(maNv)
            };
            _context.TongGioThieus.Add(tongGioThieu);
        }
        else
        {
            tongGioThieu.TongGioConThieu += shortfall;
        }
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
        
        string subject = "";
        string type = "";
        
        switch (trangThai)
        {
            case "TC3":
                subject = "Thông báo duyệt tăng ca";
                type = "tăng ca";
                break;
            case "LB3":
                subject = "Thông báo duyệt làm bù";
                type = "làm bù";
                break;
            case "CC3":
                subject = "Thông báo duyệt chấm công";
                type = "chấm công";
                break;
            default:
                subject = "Thông báo kết quả duyệt";
                type = "yêu cầu";
                break;
        }
        
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #2c3e50; margin: 0;'>{subject}</h2>
                </div>
                <p>Kính gửi <strong>{employeeName}</strong>,</p>
                <p>Phòng Nhân sự trân trọng thông báo:</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #28a745;'>
                    <p>Yêu cầu {type} của bạn vào ngày <strong>{ngay:dd/MM/yyyy}</strong> đã được <span style='color: #28a745; font-weight: bold;'>DUYỆT</span> bởi Ban Giám đốc.</p>
                </div>
                <p>Vui lòng kiểm tra lại thông tin trên hệ thống.</p>
                <p>Trân trọng,</p>
                <p><strong>Phòng Nhân sự</strong></p>
                <hr style='border: 1px solid #e0e0e0; margin: 20px 0;'>
                <p style='color: #666; font-size: 12px;'>Đây là email tự động, vui lòng không trả lời email này.</p>
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

    private void SendRejectionEmail(string recipientEmail, string employeeName, DateOnly ngay, string trangThai, string loaiYeuCau, string ghiChu)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"];
        var senderPassword = emailSettings["SenderPassword"];
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"]);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("HR Department", senderEmail));
        message.To.Add(new MailboxAddress(employeeName, recipientEmail));
        
        string subject = "";
        switch (trangThai)
        {
            case "TC4":
                subject = "Thông báo từ chối tăng ca";
                break;
            case "LB4":
                subject = "Thông báo từ chối làm bù";
                break;
            case "CC4":
                subject = "Thông báo từ chối chấm công";
                break;
            default:
                subject = "Thông báo từ chối yêu cầu";
                break;
        }
        
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #2c3e50; margin: 0;'>{subject}</h2>
                </div>
                <p>Kính gửi <strong>{employeeName}</strong>,</p>
                <p>Phòng Nhân sự trân trọng thông báo:</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #dc3545;'>
                    <p>Yêu cầu {loaiYeuCau} của bạn vào ngày <strong>{ngay:dd/MM/yyyy}</strong> đã bị <span style='color: #dc3545; font-weight: bold;'>TỪ CHỐI</span> bởi Ban Giám đốc.</p>
                    <div style='margin-top: 15px; padding-top: 15px; border-top: 1px solid #dee2e6;'>
                        <p style='margin: 0;'><strong>Lý do từ chối:</strong></p>
                        <p style='color: #666; margin: 5px 0 0 0;'>{ghiChu}</p>
                    </div>
                </div>
                <p>Vui lòng kiểm tra lại thông tin trên hệ thống.</p>
                <p>Trân trọng,</p>
                <p><strong>Phòng Nhân sự</strong></p>
                <hr style='border: 1px solid #e0e0e0; margin: 20px 0;'>
                <p style='color: #666; font-size: 12px;'>Đây là email tự động, vui lòng không trả lời email này.</p>
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

    private void SendBatchRejectionEmail(string recipientEmail, string employeeName, string loaiYeuCau, List<string> details)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"];
        var senderPassword = emailSettings["SenderPassword"];
        var smtpServer = emailSettings["SmtpServer"];
        var port = int.Parse(emailSettings["Port"]);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("HR Department", senderEmail));
        message.To.Add(new MailboxAddress(employeeName, recipientEmail));
        
        string subject = $"Thông báo từ chối {loaiYeuCau} hàng loạt";
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        var detailsHtml = string.Join("", details.Select(d => $"<li style='margin-bottom: 8px;'>{d}</li>"));
        
        bodyBuilder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <h2 style='color: #2c3e50; margin: 0;'>{subject}</h2>
                </div>
                <p>Kính gửi <strong>{employeeName}</strong>,</p>
                <p>Phòng Nhân sự trân trọng thông báo:</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #dc3545;'>
                    <p>Các yêu cầu {loaiYeuCau} của bạn đã bị <span style='color: #dc3545; font-weight: bold;'>TỪ CHỐI</span> bởi Ban Giám đốc:</p>
                    <ul style='color: #666; margin: 10px 0; padding-left: 20px;'>{detailsHtml}</ul>
                </div>
                <p>Vui lòng kiểm tra lại thông tin trên hệ thống.</p>
                <p>Trân trọng,</p>
                <p><strong>Phòng Nhân sự</strong></p>
                <hr style='border: 1px solid #e0e0e0; margin: 20px 0;'>
                <p style='color: #666; font-size: 12px;'>Đây là email tự động, vui lòng không trả lời email này.</p>
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

    private void CapNhatTrangThaiChamCong(int maChamCong, string trangThai)
    {
        var chamCong = _context.ChamCongs.FirstOrDefault(cc => cc.MaChamCong == maChamCong);
        if (chamCong != null)
        {
            chamCong.TrangThai = trangThai;
        }
    }

    // 🔹 Lấy danh sách làm bù của nhân viên (Manager)
    [HttpGet("GetMakeupRecords")]
    public IActionResult GetMakeupRecords(int maNv)
    {
        var makeupRecords = _context.LamBus
            .Where(lb => lb.MaNV == maNv && (lb.TrangThai == null || lb.TrangThai == "LB1"))
            .Select(lb => new
            {
                lb.MaLamBu,
                lb.NgayLamViec,
                lb.GioVao,
                lb.GioRa,
                lb.TongGio,
                TrangThai = lb.TrangThai ?? "LB1",
                lb.GhiChu,
                lb.MaNvDuyet
            })
            .ToList();

        return Ok(new { success = true, records = makeupRecords });
    }

    // 🔹 Duyệt hoặc từ chối làm bù (Manager, đơn lẻ)
    [HttpPost("ApproveMakeup")]
    public IActionResult ApproveMakeup(ApproveAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var lamBu = _context.LamBus.FirstOrDefault(lb => lb.MaLamBu == request.MaChamCong);
        if (lamBu == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy yêu cầu làm bù." });
        }

        if (request.TrangThai == "LB4")
        {
            lamBu.TrangThai = "LB4";
            lamBu.GhiChu = request.GhiChu ?? "Không có ghi chú";
            lamBu.MaNvDuyet = maNv;

            var employee = _context.NhanViens.Find(lamBu.MaNV);
            if (employee != null)
            {
                SendRejectionEmail(employee.Email, employee.HoTen, lamBu.NgayLamViec, "LB4", "làm bù", lamBu.GhiChu);
            }

            _context.SaveChanges();
            return Ok(new { success = true, message = "Đã từ chối làm bù." });
        }

        lamBu.TrangThai = "LB2";
        lamBu.MaNvDuyet = maNv;
        _context.SaveChanges();

        return Ok(new { success = true, message = "Duyệt làm bù thành công." });
    }

    // 🔹 Duyệt hoặc từ chối nhiều bản ghi làm bù (Manager)
    [HttpPost("ApproveMultipleMakeup")]
    public IActionResult ApproveMultipleMakeup([FromBody] ApproveMultipleAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var failedRecords = new List<int>();
                var rejectionDetails = new List<(string Email, string HoTen, DateOnly Ngay, string GhiChu)>();

                foreach (var maLamBu in request.MaChamCongList)
                {
                    var lamBu = _context.LamBus.FirstOrDefault(lb => lb.MaLamBu == maLamBu);
                    if (lamBu == null)
                    {
                        failedRecords.Add(maLamBu);
                        continue;
                    }

                    if (lamBu.TrangThai != "LB1" && lamBu.TrangThai != null)
                    {
                        failedRecords.Add(maLamBu);
                        continue;
                    }

                    if (request.TrangThai == "LB4")
                    {
                        lamBu.TrangThai = "LB4";
                        lamBu.GhiChu = request.GhiChu ?? "Không có ghi chú";
                        lamBu.MaNvDuyet = maNv;

                        var employee = _context.NhanViens.Find(lamBu.MaNV);
                        if (employee != null)
                        {
                            rejectionDetails.Add((employee.Email, employee.HoTen, lamBu.NgayLamViec, lamBu.GhiChu));
                        }
                    }
                    else if (request.TrangThai == "LB2")
                    {
                        lamBu.TrangThai = "LB2";
                        lamBu.MaNvDuyet = maNv;
                    }
                }

                _context.SaveChanges();

                if (request.TrangThai == "LB4" && rejectionDetails.Any())
                {
                    foreach (var group in rejectionDetails.GroupBy(d => d.Email))
                    {
                        var email = group.Key;
                        var hoTen = group.First().HoTen;
                        var details = group.Select(d => $"Ngày {d.Ngay:dd/MM/yyyy}: {d.GhiChu}").ToList();
                        SendBatchRejectionEmail(email, hoTen, "làm bù", details);
                    }
                }

                transaction.Commit();
                var baseMessage = request.TrangThai == "LB2" ? "Duyệt làm bù thành công." : "Đã từ chối làm bù.";
                var message = failedRecords.Any()
                    ? $"{baseMessage} Tuy nhiên, các bản ghi {string.Join(", ", failedRecords)} không được cập nhật."
                    : baseMessage;
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    // 🔹 Lấy danh sách làm bù của nhân viên (Director)
    [HttpGet("GetMakeupRecordsDerector")]
    public IActionResult GetMakeupRecordsDerector(int maNv)
    {
        var makeupRecords = _context.LamBus
            .Where(lb => lb.MaNV == maNv && (lb.TrangThai == null || lb.TrangThai == "LB2"))
            .Select(lb => new
            {
                lb.MaLamBu,
                lb.NgayLamViec,
                lb.GioVao,
                lb.GioRa,
                lb.TongGio,
                TrangThai = lb.TrangThai ?? "LB2",
                lb.GhiChu,
                lb.MaNvDuyet
            })
            .ToList();

        return Ok(new { success = true, records = makeupRecords });
    }

    // 🔹 Duyệt hoặc từ chối làm bù (Director, đơn lẻ)
    [HttpPost("ApproveMakeupDerector")]
    public IActionResult ApproveMakeupDerector(ApproveAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        var lamBu = _context.LamBus.FirstOrDefault(lb => lb.MaLamBu == request.MaChamCong);
        if (lamBu == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy yêu cầu làm bù." });
        }

        var employee = _context.NhanViens.Find(lamBu.MaNV);
        if (employee == null)
        {
            return BadRequest(new { success = false, message = "Không tìm thấy nhân viên." });
        }

        if (request.TrangThai == "Đã duyệt")
        {
            lamBu.TrangThai = "LB3";
            lamBu.MaNvDuyet = maNv;

            // Cập nhật số giờ thiếu và giờ làm bù
            if (lamBu.TongGio.HasValue)
            {
                var firstDayOfMonth = new DateOnly(lamBu.NgayLamViec.Year, lamBu.NgayLamViec.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                var tongGioThieu = _context.TongGioThieus
                    .FirstOrDefault(t => t.MaNv == lamBu.MaNV &&
                                       t.NgayBatDauThieu == firstDayOfMonth &&
                                       t.NgayKetThucThieu == lastDayOfMonth);

                if (tongGioThieu == null)
                {
                    tongGioThieu = new TongGioThieu
                    {
                        MaNv = lamBu.MaNV,
                        NgayBatDauThieu = firstDayOfMonth,
                        NgayKetThucThieu = lastDayOfMonth,
                        TongGioConThieu = 0m,
                        TongGioLamBu = lamBu.TongGio.Value,
                        MaNvNavigation = employee
                    };
                    _context.TongGioThieus.Add(tongGioThieu);
                }
                else
                {
                    tongGioThieu.TongGioLamBu += lamBu.TongGio.Value;
                }

                var gioThieu = _context.GioThieus
                    .FirstOrDefault(gt => gt.MaNv == lamBu.MaNV && gt.NgayThieu == lamBu.NgayLamViec);

                if (gioThieu != null)
                {
                    gioThieu.TongGioThieu = Math.Max(0, gioThieu.TongGioThieu - lamBu.TongGio.Value);
                    if (gioThieu.TongGioThieu == 0)
                    {
                        _context.GioThieus.Remove(gioThieu);
                    }
                }
            }

            SendApprovalEmail(employee.Email, employee.HoTen, lamBu.NgayLamViec, "LB3");
        }
        else if (request.TrangThai == "Từ chối")
        {
            lamBu.TrangThai = "LB4";
            lamBu.GhiChu = request.GhiChu ?? "Không có ghi chú";
            lamBu.MaNvDuyet = maNv;
            SendRejectionEmail(employee.Email, employee.HoTen, lamBu.NgayLamViec, "LB4", "làm bù", lamBu.GhiChu);
        }

        _context.SaveChanges();
        var message = request.TrangThai == "Đã duyệt" ? "Duyệt làm bù thành công." : "Đã từ chối làm bù.";
        return Ok(new { success = true, message });
    }

    // 🔹 Duyệt hoặc từ chối nhiều bản ghi làm bù (Director)
    [HttpPost("ApproveMultipleMakeupDerector")]
    public IActionResult ApproveMultipleMakeupDerector([FromBody] ApproveMultipleAttendanceRequestDTO request)
    {
        int maNv = (int)GetMaNvFromClaims();
        if (maNv == null)
            return Unauthorized("Không tìm thấy mã nhân viên trong claims.");

        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var failedRecords = new List<int>();
                var rejectionDetails = new List<(string Email, string HoTen, DateOnly Ngay, string GhiChu)>();

                foreach (var maLamBu in request.MaChamCongList)
                {
                    var lamBu = _context.LamBus.FirstOrDefault(lb => lb.MaLamBu == maLamBu);
                    if (lamBu == null)
                    {
                        failedRecords.Add(maLamBu);
                        continue;
                    }

                    if (lamBu.TrangThai != "LB2" && lamBu.TrangThai != null)
                    {
                        failedRecords.Add(maLamBu);
                        continue;
                    }

                    var employee = _context.NhanViens.Find(lamBu.MaNV);
                    if (employee == null)
                    {
                        failedRecords.Add(maLamBu);
                        continue;
                    }

                    if (request.TrangThai == "Từ chối")
                    {
                        lamBu.TrangThai = "LB4";
                        lamBu.GhiChu = request.GhiChu ?? "Không có ghi chú";
                        lamBu.MaNvDuyet = maNv;
                        rejectionDetails.Add((employee.Email, employee.HoTen, lamBu.NgayLamViec, lamBu.GhiChu));
                    }
                    else if (request.TrangThai == "Đã duyệt")
                    {
                        lamBu.TrangThai = "LB3";
                        lamBu.MaNvDuyet = maNv;

                        // Cập nhật số giờ thiếu và giờ làm bù
                        if (lamBu.TongGio.HasValue)
                        {
                            var firstDayOfMonth = new DateOnly(lamBu.NgayLamViec.Year, lamBu.NgayLamViec.Month, 1);
                            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                            var tongGioThieu = _context.TongGioThieus
                                .FirstOrDefault(t => t.MaNv == lamBu.MaNV &&
                                                   t.NgayBatDauThieu == firstDayOfMonth &&
                                                   t.NgayKetThucThieu == lastDayOfMonth);

                            if (tongGioThieu == null)
                            {
                                tongGioThieu = new TongGioThieu
                                {
                                    MaNv = lamBu.MaNV,
                                    NgayBatDauThieu = firstDayOfMonth,
                                    NgayKetThucThieu = lastDayOfMonth,
                                    TongGioConThieu = 0m,
                                    TongGioLamBu = lamBu.TongGio.Value,
                                    MaNvNavigation = employee
                                };
                                _context.TongGioThieus.Add(tongGioThieu);
                            }
                            else
                            {
                                tongGioThieu.TongGioLamBu += lamBu.TongGio.Value;
                            }

                            var gioThieu = _context.GioThieus
                                .FirstOrDefault(gt => gt.MaNv == lamBu.MaNV && gt.NgayThieu == lamBu.NgayLamViec);

                            if (gioThieu != null)
                            {
                                gioThieu.TongGioThieu = Math.Max(0, gioThieu.TongGioThieu - lamBu.TongGio.Value);
                                if (gioThieu.TongGioThieu == 0)
                                {
                                    _context.GioThieus.Remove(gioThieu);
                                }
                            }
                        }

                        SendApprovalEmail(employee.Email, employee.HoTen, lamBu.NgayLamViec, "LB3");
                    }
                }

                _context.SaveChanges();

                if (request.TrangThai == "Từ chối" && rejectionDetails.Any())
                {
                    foreach (var group in rejectionDetails.GroupBy(d => d.Email))
                    {
                        var email = group.Key;
                        var hoTen = group.First().HoTen;
                        var details = group.Select(d => $"Ngày {d.Ngay:dd/MM/yyyy}: {d.GhiChu}").ToList();
                        SendBatchRejectionEmail(email, hoTen, "làm bù", details);
                    }
                }

                transaction.Commit();
                var baseMessage = request.TrangThai == "Đã duyệt" ? "Duyệt làm bù thành công." : "Đã từ chối làm bù.";
                var message = failedRecords.Any()
                    ? $"{baseMessage} Tuy nhiên, các bản ghi {string.Join(", ", failedRecords)} không được cập nhật do nhân viên còn giờ thiếu chưa được bù."
                    : baseMessage;
                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}

public class ApproveAttendanceRequestDTO
{
    public int MaChamCong { get; set; }
    public string TrangThai { get; set; }
    public string GhiChu { get; set; }
}

public class ApproveMultipleAttendanceRequestDTO
{
    public List<int> MaChamCongList { get; set; }
    public string TrangThai { get; set; }
    public string GhiChu { get; set; }
}