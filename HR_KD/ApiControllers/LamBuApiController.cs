using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    [Authorize]
    [Route("api/LamBu")]
    [ApiController]
    public class LamBuApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public LamBuApiController(HrDbContext context)
        {
            _context = context;
        }

        private int? GetMaNvFromClaims()
        {
            var maNvClaim = User.FindFirst("MaNV")?.Value;
            return int.TryParse(maNvClaim, out int maNv) ? maNv : null;
        }

        // GET: api/LamBu
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LamBu>>> GetLamBu()
        {
            var maNv = GetMaNvFromClaims();
            if (!maNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Mã nhân viên không hợp lệ." });
            }

            return await _context.LamBus
                .Where(lb => lb.MaNV == maNv.Value)
                .ToListAsync();
        }

        // GET: api/LamBu/GetRemainingHours/{maNv}
        [HttpGet("GetRemainingHours/{maNv}")]
        public async Task<ActionResult> GetRemainingHours(int maNv)
        {
            var userMaNv = GetMaNvFromClaims();
            if (!userMaNv.HasValue || userMaNv.Value != maNv)
            {
                return Unauthorized(new { success = false, message = "Không có quyền truy cập dữ liệu của nhân viên này." });
            }

            try
            {
                var currentDate = DateOnly.FromDateTime(DateTime.Now);

                var tongGioThieu = await _context.TongGioThieus
                    .Where(t => t.MaNv == maNv && t.NgayBatDauThieu <= currentDate && t.NgayKetThucThieu >= currentDate)
                    .FirstOrDefaultAsync();

                if (tongGioThieu == null)
                {
                    return Ok(new
                    {
                        success = true,
                        remainingHours = 0m,
                        tongGioConThieu = 0m,
                        tongGioLamBu = 0m
                    });
                }

                var remainingHours = tongGioThieu.TongGioConThieu - tongGioThieu.TongGioLamBu;

                return Ok(new
                {
                    success = true,
                    remainingHours = remainingHours,
                    tongGioConThieu = tongGioThieu.TongGioConThieu,
                    tongGioLamBu = tongGioThieu.TongGioLamBu
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // POST: api/LamBu/SubmitLamBu
        [HttpPost("SubmitLamBu")]
        public async Task<ActionResult> SubmitLamBu([FromBody] LamBuRequest request)
        {
            var userMaNv = GetMaNvFromClaims();
            if (!userMaNv.HasValue)
            {
                return Unauthorized(new { success = false, message = "Mã nhân viên không hợp lệ." });
            }

            if (request == null || request.LamBu == null || !request.LamBu.Any())
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            if (request.LamBu.Any(lb => lb.MaNV != userMaNv.Value))
            {
                return Unauthorized(new { success = false, message = "Không có quyền gửi dữ liệu cho nhân viên khác." });
            }

            try
            {
                // Fetch active ChamCongGioRaVao configuration
                var workTimeConfig = await _context.ChamCongGioRaVaos
                    .Where(c => c.KichHoat)
                    .FirstOrDefaultAsync();

                if (workTimeConfig == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy cấu hình giờ ra vào đang kích hoạt." });
                }

                var gioVaoConfig = workTimeConfig.GioVao;
                var gioRaConfig = workTimeConfig.GioRa;

                // Fetch holiday data
                var holidays = await _context.NgayLes
                    .Where(h => request.LamBu.Select(lb => lb.NgayLamViec).Contains(h.NgayLe1))
                    .ToDictionaryAsync(h => h.NgayLe1, h => h.TrangThai);

                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var tongGioThieu = await _context.TongGioThieus
                    .Where(t => t.MaNv == userMaNv.Value && t.NgayBatDauThieu <= currentDate && t.NgayKetThucThieu >= currentDate)
                    .FirstOrDefaultAsync();

                decimal remainingHours = tongGioThieu?.TongGioConThieu - tongGioThieu?.TongGioLamBu ?? 0;

                decimal totalSubmittedHours = request.LamBu.Sum(lb => lb.TongGio ?? 0);

                if (totalSubmittedHours > remainingHours)
                {
                    return BadRequest(new { success = false, message = $"Tổng giờ làm bù ({totalSubmittedHours}h) không được vượt quá số giờ còn thiếu ({remainingHours}h)." });
                }

                foreach (var lamBu in request.LamBu)
                {
                    var ngayLamViec = lamBu.NgayLamViec;
                    var dayOfWeek = ngayLamViec.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                    var gioVao = lamBu.GioVao;
                    var gioRa = lamBu.GioRa;

                    // Check holiday or bonus day status
                    var isHoliday = holidays.TryGetValue(ngayLamViec, out var trangThai) && trangThai == "NL4";
                    var isBonusDay = holidays.TryGetValue(ngayLamViec, out trangThai) && trangThai == "NT";

                    // Validation for weekdays (Monday-Friday, including bonus days)
                    if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday && !isHoliday)
                    {
                        if (!gioVao.HasValue || !gioRa.HasValue)
                        {
                            return BadRequest(new { success = false, message = $"Giờ vào và giờ ra không được để trống cho ngày {ngayLamViec}." });
                        }

                        // Check that GioVao matches GioRa from ChamCongGioRaVao
                        if (gioVao.Value != gioRaConfig)
                        {
                            return BadRequest(new { success = false, message = $"Giờ vào làm bù cho ngày {ngayLamViec} phải là {gioRaConfig}." });
                        }

                        // Calculate hours
                        var hours = (gioRa.Value - gioVao.Value).TotalHours;
                        if (hours < 0) hours += 24;
                        if (isBonusDay && hours > 4) hours -= 1; // Apply 1-hour break for bonus days > 4 hours

                        // Validate hours <= 4
                        if (hours > 4)
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào ngày {ngayLamViec} không được vượt quá 4 giờ." });
                        }

                        lamBu.TongGio = (decimal)Math.Round(hours, 2);
                    }
                    // Validation for weekends (Saturday-Sunday)
                    else if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    {
                        if (!gioVao.HasValue || !gioRa.HasValue)
                        {
                            return BadRequest(new { success = false, message = $"Giờ vào và giờ ra không được để trống cho ngày {ngayLamViec}." });
                        }

                        if (gioVao < TimeOnly.Parse("08:00") || gioVao > TimeOnly.Parse("22:00") ||
                            gioRa < TimeOnly.Parse("08:00") || gioRa > TimeOnly.Parse("22:00"))
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào thứ Bảy và Chủ Nhật chỉ được phép từ 08:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        var hours = (gioRa.Value - gioVao.Value).TotalHours;
                        if (hours < 0) hours += 24;
                        if (hours > 4) hours -= 1; // Apply 1-hour break for weekend shifts > 4 hours

                        if (hours > 8)
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào cuối tuần không được vượt quá 8 giờ. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        lamBu.TongGio = (decimal)Math.Round(hours, 2);
                    }
                    // Validation for holidays
                    else if (isHoliday)
                    {
                        if (!gioVao.HasValue || !gioRa.HasValue)
                        {
                            return BadRequest(new { success = false, message = $"Giờ vào và giờ ra không được để trống cho ngày {ngayLamViec}." });
                        }

                        if (gioVao < gioVaoConfig || gioVao > gioRaConfig || gioRa < gioVaoConfig || gioRa > gioRaConfig)
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào ngày lễ phải trong khoảng {gioVaoConfig} đến {gioRaConfig}. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        var hours = (gioRa.Value - gioVao.Value).TotalHours;
                        if (hours < 0) hours += 24;
                        if (hours > 4) hours -= 1; // Apply 1-hour break for holiday shifts > 4 hours

                        if (hours > 8)
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào ngày lễ không được vượt quá 8 giờ. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        lamBu.TongGio = (decimal)Math.Round(hours, 2);
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = $"Không được phép làm bù vào ngày {ngayLamViec}." });
                    }

                    // Check for conflicts with overtime (TangCa)
                    var conflictingTangCa = await _context.TangCas
                        .Where(tc => tc.NgayTangCa == ngayLamViec && tc.MaNv == lamBu.MaNV)
                        .ToListAsync();

                    foreach (var tangCa in conflictingTangCa)
                    {
                        TimeOnly tangCaStart = tangCa.GioVao ?? TimeOnly.Parse("18:00");
                        TimeOnly tangCaEnd = tangCa.GioRa ?? tangCaStart.AddHours((double)tangCa.SoGioTangCa);

                        bool crossesMidnightTangCa = tangCaEnd < tangCaStart;
                        bool crossesMidnightLamBu = gioRa < gioVao;

                        double lamBuStartMinutes = gioVao.Value.Hour * 60 + gioVao.Value.Minute;
                        double lamBuEndMinutes = gioRa.Value.Hour * 60 + gioRa.Value.Minute;
                        double tangCaStartMinutes = tangCaStart.Hour * 60 + tangCaStart.Minute;
                        double tangCaEndMinutes = tangCaEnd.Hour * 60 + tangCaEnd.Minute;

                        if (crossesMidnightLamBu) lamBuEndMinutes += 24 * 60;
                        if (crossesMidnightTangCa) tangCaEndMinutes += 24 * 60;

                        bool hasOverlap = (lamBuStartMinutes >= tangCaStartMinutes && lamBuStartMinutes <= tangCaEndMinutes) ||
                                         (lamBuEndMinutes >= tangCaStartMinutes && lamBuEndMinutes <= tangCaEndMinutes) ||
                                         (lamBuStartMinutes <= tangCaStartMinutes && lamBuEndMinutes >= tangCaEndMinutes);

                        if (hasOverlap)
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào ngày {ngayLamViec} trùng với giờ tăng ca từ {tangCaStart} đến {tangCaEnd}. Vui lòng kiểm tra lại." });
                        }
                    }

                    lamBu.TrangThai = "LB1";
                    _context.LamBus.Add(lamBu);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Lưu dữ liệu làm bù thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }

    public class LamBuRequest
    {
        public List<LamBu> LamBu { get; set; }
    }
}