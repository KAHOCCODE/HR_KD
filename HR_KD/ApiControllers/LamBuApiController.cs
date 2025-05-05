using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LamBuApiController : ControllerBase
    {
        private readonly HrDbContext _context; // Replace with your actual DbContext

        public LamBuApiController(HrDbContext context)
        {
            _context = context;
        }

        // GET: api/LamBu
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LamBu>>> GetLamBu()
        {
            return await _context.LamBus.ToListAsync();
        }

        // POST: api/LamBu/SubmitLamBu
        [HttpPost("SubmitLamBu")]
        public async Task<ActionResult> SubmitLamBu([FromBody] LamBuRequest request)
        {
            if (request == null || (request.LamBu == null && request.LamBuBanDem == null) || (!request.LamBu.Any() && !request.LamBuBanDem.Any()))
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                // Process day compensatory work
                if (request.LamBu != null)
                {
                    foreach (var lamBu in request.LamBu)
                    {
                        // Validate day compensatory work conditions
                        var ngayLamViec = lamBu.NgayLamViec;
                        var dayOfWeek = ngayLamViec.ToDateTime(TimeOnly.MinValue).DayOfWeek;
                        var gioVao = lamBu.GioVao;
                        var gioRa = lamBu.GioRa;

                        // Check time restrictions based on day of the week
                        if (dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday)
                        {
                            // Monday to Friday: 18:00 - 22:00
                            if (gioVao < TimeOnly.Parse("18:00") || gioVao > TimeOnly.Parse("22:00") ||
                                gioRa < TimeOnly.Parse("18:00") || gioRa > TimeOnly.Parse("22:00"))
                            {
                                return BadRequest(new { success = false, message = $"Làm bù từ thứ Hai đến thứ Sáu chỉ được phép từ 18:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                            }
                        }
                        else if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                        {
                            // Saturday and Sunday: 08:00 - 18:00 or 18:00 - 22:00
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

                        // Check for overtime conflicts
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

                        // Calculate total hours
                        if (gioVao.HasValue && gioRa.HasValue)
                        {
                            var hours = (gioRa.Value - gioVao.Value).TotalHours;
                            if (hours < 0) hours += 24;
                            if (hours >= 8) hours -= 2;
                            lamBu.TongGio = (decimal)Math.Round(hours, 2);
                        }

                        lamBu.TrangThai = "LS1";
                        _context.LamBus.Add(lamBu);
                    }
                }

                // Process night compensatory work
                if (request.LamBuBanDem != null)
                {
                    foreach (var lamBu in request.LamBuBanDem)
                    {
                        // Validate night compensatory work conditions
                        var ngayLamViec = lamBu.NgayLamViec;
                        var gioVao = lamBu.GioVao;
                        var gioRa = lamBu.GioRa;

                        // Night compensatory work: 18:00 - 22:00
                        if (gioVao < TimeOnly.Parse("18:00") || gioVao > TimeOnly.Parse("22:00") ||
                            gioRa < TimeOnly.Parse("18:00") || gioRa > TimeOnly.Parse("22:00"))
                        {
                            return BadRequest(new { success = false, message = $"Làm bù ban đêm chỉ được phép từ 18:00 đến 22:00. Ngày {ngayLamViec} không hợp lệ." });
                        }

                        // Check for overtime conflicts
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

                        // Check for day compensatory work conflicts
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

                        // Calculate total hours
                        if (gioVao.HasValue && gioRa.HasValue)
                        {
                            var hours = (gioRa.Value - gioVao.Value).TotalHours;
                            if (hours < 0) hours += 24;
                            lamBu.TongGio = (decimal)Math.Round(hours, 2);
                        }

                        lamBu.TrangThai = "LBBD"; // Night compensatory work status
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