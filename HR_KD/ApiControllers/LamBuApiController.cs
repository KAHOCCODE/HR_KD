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
        public async Task<ActionResult<decimal>> GetRemainingHours(int maNv)
        {
            var userMaNv = GetMaNvFromClaims();
            if (!userMaNv.HasValue || userMaNv.Value != maNv)
            {
                return Unauthorized(new { success = false, message = "Không có quyền truy cập dữ liệu của nhân viên này." });
            }

            try
            {
                var currentDate = DateOnly.FromDateTime(DateTime.Now);

                // Query TongGioThieu for the employee where the current date is within NgayBatDauThieu and NgayKetThucThieu
                var tongGioThieu = await _context.TongGioThieus
                    .Where(t => t.MaNv == maNv && t.NgayBatDauThieu <= currentDate && t.NgayKetThucThieu >= currentDate)
                    .FirstOrDefaultAsync();

                // If no record is found, return zeroed values
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

                // Calculate remaining hours
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

            if (request == null || (request.LamBu == null && request.LamBuBanDem == null) || (!request.LamBu.Any() && !request.LamBuBanDem.Any()))
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            if (request.LamBu?.Any(lb => lb.MaNV != userMaNv.Value) == true ||
                request.LamBuBanDem?.Any(lb => lb.MaNV != userMaNv.Value) == true)
            {
                return Unauthorized(new { success = false, message = "Không có quyền gửi dữ liệu cho nhân viên khác." });
            }

            try
            {
                // Fetch remaining hours
                var currentDate = DateOnly.FromDateTime(DateTime.Now);
                var tongGioThieu = await _context.TongGioThieus
                    .Where(t => t.MaNv == userMaNv.Value && t.NgayBatDauThieu <= currentDate && t.NgayKetThucThieu >= currentDate)
                    .FirstOrDefaultAsync();

                decimal remainingHours = tongGioThieu?.TongGioConThieu - tongGioThieu?.TongGioLamBu ?? 0;

                // Calculate total submitted hours
                decimal totalSubmittedHours = 0;
                if (request.LamBu != null)
                {
                    totalSubmittedHours += request.LamBu.Sum(lb => lb.TongGio ?? 0);
                }
                if (request.LamBuBanDem != null)
                {
                    totalSubmittedHours += request.LamBuBanDem.Sum(lb => lb.TongGio ?? 0);
                }

                // Check if total submitted hours exceed remaining hours
                if (totalSubmittedHours > remainingHours)
                {
                    return BadRequest(new { success = false, message = $"Tổng giờ làm bù ({totalSubmittedHours}h) không được vượt quá số giờ còn thiếu ({remainingHours}h)." });
                }

                // Process day compensatory work
                if (request.LamBu != null)
                {
                    foreach (var lamBu in request.LamBu)
                    {
                        // Existing validation for day compensatory work
                        var ngayLamViec = lamBu.NgayLamViec;
                        var dayOfWeek = ngayLamViec.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                        var gioVao = lamBu.GioVao;
                        var gioRa = lamBu.GioRa;

                        if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday)
                        {
                            if (gioVao < TimeOnly.Parse("18:00") || gioVao > TimeOnly.Parse("22:00") ||
                                gioRa < TimeOnly.Parse("18:00") || gioRa > TimeOnly.Parse("22:00"))
                            {
                                return BadRequest(new { success = false, message = $"Làm bù từ thứ Hai đến thứ Sáu chỉ được phép từ 18:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                            }
                        }
                        else if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                        {
                            if ((gioVao < TimeOnly.Parse("08:00") || gioVao > TimeOnly.Parse("18:00") || gioRa < TimeOnly.Parse("08:00") || gioRa > TimeOnly.Parse("18:00")) &&
                                (gioVao < TimeOnly.Parse("18:00") || gioVao > TimeOnly.Parse("22:00") || gioRa < TimeOnly.Parse("18:00") || gioRa > TimeOnly.Parse("22:00")))
                            {
                                return BadRequest(new { success = false, message = $"Làm bù vào thứ Bảy và Chủ Nhật chỉ được phép từ 08:00 đến 18:00 hoặc 18:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                            }
                        }
                        else
                        {
                            return BadRequest(new { success = false, message = $"Không được phép làm bù vào ngày {ngayLamViec}." });
                        }

                        var conflictingTangCa = await _context.TangCas
                            .Where(tc => tc.NgayTangCa == ngayLamViec && tc.MaNv == lamBu.MaNV)
                            .ToListAsync();

                        foreach (var tangCa in conflictingTangCa)
                        {
                            var tangCaStart = TimeOnly.Parse("18:00");
                            var tangCaEnd = tangCaStart.AddHours((double)tangCa.SoGioTangCa);

                            if ((gioVao >= tangCaStart && gioVao <= tangCaEnd) || (gioRa >= tangCaStart && gioRa <= tangCaEnd) ||
                                (gioVao <= tangCaStart && gioRa >= tangCaEnd))
                    {
                                return BadRequest(new { success = false, message = $"Làm bù vào ngày {ngayLamViec} trùng với giờ tăng ca. Vui lòng kiểm tra lại." });
                            }
                        }

                        if (gioVao.HasValue && gioRa.HasValue)
                        {
                            var hours = (gioRa.Value - gioVao.Value).TotalHours;
                            if (hours < 0) hours += 24;
                            if (hours >= 8) hours -= 2;
                            lamBu.TongGio = (decimal)Math.Round(hours, 2);
                        }

                        lamBu.TrangThai = "LB1";
                        _context.LamBus.Add(lamBu);
                    }
                }

                // Process night compensatory work
                if (request.LamBuBanDem != null)
                {
                    foreach (var lamBu in request.LamBuBanDem)
                    {
                        // Existing validation for night compensatory work
                        var ngayLamViec = lamBu.NgayLamViec;
                        var gioVao = lamBu.GioVao;
                        var gioRa = lamBu.GioRa;

                        if (gioVao < TimeOnly.Parse("18:00") || gioVao > TimeOnly.Parse("22:00") ||
                            gioRa < TimeOnly.Parse("18:00") || gioRa > TimeOnly.Parse("22:00"))
                        {
                            return BadRequest(new { success = false, message = $"Làm bù ban đêm chỉ được phép từ 18:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        var conflictingTangCa = await _context.TangCas
                            .Where(tc => tc.NgayTangCa == ngayLamViec && tc.MaNv == lamBu.MaNV)
                            .ToListAsync();

                        foreach (var tangCa in conflictingTangCa)
                        {
                            var tangCaStart = TimeOnly.Parse("18:00");
                            var tangCaEnd = tangCaStart.AddHours((double)tangCa.SoGioTangCa);

                            if ((gioVao >= tangCaStart && gioVao <= tangCaEnd) || (gioRa >= tangCaStart && gioRa <= tangCaEnd) ||
                                (gioVao <= tangCaStart && gioRa >= tangCaEnd))
                            {
                                return BadRequest(new { success = false, message = $"Làm bù ban đêm vào ngày {ngayLamViec} trùng với giờ tăng ca. Vui lòng kiểm tra lại." });
                            }
                        }

                        var conflictingLamBu = request.LamBu?.Where(lb => lb.NgayLamViec == ngayLamViec && lb.MaNV == lamBu.MaNV).ToList() ?? new List<LamBu>();
                        foreach (var dayLamBu in conflictingLamBu)
                        {
                            if ((gioVao >= dayLamBu.GioVao && gioVao <= dayLamBu.GioRa) ||
                                (gioRa >= dayLamBu.GioVao && gioRa <= dayLamBu.GioRa) ||
                                (gioVao <= dayLamBu.GioVao && gioRa >= dayLamBu.GioRa))
                            {
                                return BadRequest(new { success = false, message = $"Làm bù ban đêm vào ngày {ngayLamViec} trùng với giờ làm bù ban ngày. Vui lòng kiểm tra lại." });
                            }
                        }

                        if (gioVao.HasValue && gioRa.HasValue)
                        {
                            var hours = (gioRa.Value - gioVao.Value).TotalHours;
                            if (hours < 0) hours += 24;
                            lamBu.TongGio = (decimal)Math.Round(hours, 2);
                        }

                        lamBu.TrangThai = "LB1";
                        _context.LamBus.Add(lamBu);
                    }
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
        public List<LamBu> LamBuBanDem { get; set; }
    }
}