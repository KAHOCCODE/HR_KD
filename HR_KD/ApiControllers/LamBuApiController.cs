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

                    // Validation for weekdays (Monday-Friday): 18:00-22:00, max 4 hours
                    if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday)
                    {
                        if (gioVao < TimeOnly.Parse("18:00") || gioVao > TimeOnly.Parse("22:00") ||
                            gioRa < TimeOnly.Parse("18:00") || gioRa > TimeOnly.Parse("22:00"))
                        {
                            return BadRequest(new { success = false, message = $"Làm bù từ thứ Hai đến thứ Sáu chỉ được phép từ 18:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        var hours = (gioRa.Value - gioVao.Value).TotalHours;
                        if (hours < 0) hours += 24;
                        if (hours > 4)
                        {
                            return BadRequest(new { success = false, message = $"Làm bù từ thứ Hai đến thứ Sáu không được vượt quá 4 giờ. Ngày {ngayLamViec} không hợp lệ." });
                        }
                    }
                    // Validation for weekends (Saturday-Sunday): 08:00-22:00, 1-hour break if over 4 hours
                    else if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                    {
                        if (gioVao < TimeOnly.Parse("08:00") || gioVao > TimeOnly.Parse("22:00") ||
                            gioRa < TimeOnly.Parse("08:00") || gioRa > TimeOnly.Parse("22:00"))
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào thứ Bảy và Chủ Nhật chỉ được phép từ 08:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                        }
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
                        var tangCaStart = TimeOnly.Parse("18:00");
                        var tangCaEnd = tangCaStart.AddHours((double)tangCa.SoGioTangCa);

                        if ((gioVao >= tangCaStart && gioVao <= tangCaEnd) ||
                            (gioRa >= tangCaStart && gioRa <= tangCaEnd) ||
                            (gioVao <= tangCaStart && gioRa >= tangCaEnd))
                        {
                            return BadRequest(new { success = false, message = $"Làm bù vào ngày {ngayLamViec} trùng với giờ tăng ca. Vui lòng kiểm tra lại." });
                        }
                    }

                    // Calculate hours, applying 1-hour break for weekend shifts over 4 hours
                    if (gioVao.HasValue && gioRa.HasValue)
                    {
                        var hours = (gioRa.Value - gioVao.Value).TotalHours;
                        if (hours < 0) hours += 24;
                        if ((dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday) && hours > 4)
                        {
                            hours -= 1; // Apply 1-hour break
                        }
                        lamBu.TongGio = (decimal)Math.Round(hours, 2);
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