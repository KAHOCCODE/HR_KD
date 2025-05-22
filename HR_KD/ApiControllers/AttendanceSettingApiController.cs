using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HR_KD.ApiControllers
{
    [Authorize(Roles = "DIRECTOR")]
    [Route("api/AttendanceSettingApi")]
    public class AttendanceSettingApiController : Controller
    {
        private readonly HrDbContext _context;

        public AttendanceSettingApiController(HrDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetChamCongGioRaVao")]
        public IActionResult GetChamCongGioRaVao()
        {
            var data = _context.ChamCongGioRaVaos
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    GioVao = x.GioVao.ToString("HH:mm:ss"),
                    GioRa = x.GioRa.ToString("HH:mm:ss"),
                    x.KichHoat,
                    x.TongGio
                })
                .ToList();
            return Json(data);
        }

        [HttpPost("SetChamCongGioRaVao")]
        public async Task<IActionResult> SetChamCongGioRaVao([FromBody] ChamCongGioRaVaoDto model)
        {
            if (model == null || !TimeOnly.TryParse(model.GioVao, out var gioVao) || !TimeOnly.TryParse(model.GioRa, out var gioRa))
            {
                return BadRequest("Invalid data or time format");
            }

            try
            {
                // If the new record is marked as active, deactivate all other records
                if (model.KichHoat)
                {
                    var activeRecords = await _context.ChamCongGioRaVaos
                        .Where(x => x.KichHoat)
                        .ToListAsync();
                    foreach (var record in activeRecords)
                    {
                        record.KichHoat = false;
                    }
                }

                // Create a new record
                var newRecord = new ChamCongGioRaVao
                {
                    GioVao = gioVao,
                    GioRa = gioRa,
                    KichHoat = model.KichHoat,
                    TongGio = model.TongGio
                };
                _context.ChamCongGioRaVaos.Add(newRecord);

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("ActivateChamCongGioRaVao/{id}")]
        public async Task<IActionResult> ActivateChamCongGioRaVao(int id)
        {
            try
            {
                var record = await _context.ChamCongGioRaVaos.FindAsync(id);
                if (record == null)
                {
                    return NotFound("Attendance record not found");
                }

                // If activating this record, deactivate all others
                if (!record.KichHoat)
                {
                    var activeRecords = await _context.ChamCongGioRaVaos
                        .Where(x => x.KichHoat && x.Id != id)
                        .ToListAsync();
                    foreach (var otherRecord in activeRecords)
                    {
                        otherRecord.KichHoat = false;
                    }
                    record.KichHoat = true;
                }
                else
                {
                    record.KichHoat = false;
                }

                _context.ChamCongGioRaVaos.Update(record);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetLichLamViec")]
        public IActionResult GetLichLamViec()
        {
            var data = _context.LichLamViecs.ToList();
            return Json(data);
        }

        [HttpPost("ActivateLichLamViec/{tenLich}")]
        public async Task<IActionResult> ActivateLichLamViec(string tenLich)
        {
            var schedule = await _context.LichLamViecs
                .FirstOrDefaultAsync(x => x.TenLich == tenLich);
            if (schedule == null)
            {
                return NotFound("Schedule not found");
            }

            foreach (var item in _context.LichLamViecs)
            {
                item.KichHoat = (item.TenLich == tenLich);
            }

            await _context.SaveChangesAsync();
            return Json(_context.LichLamViecs.ToList());
        }

        [HttpGet("GetTiLeTangCa")]
        public IActionResult GetTiLeTangCa()
        {
            var data = _context.TiLeTangCas.OrderByDescending(x => x.Id).ToList();
            return Json(data);
        }

        [HttpPost("CreateTiLeTangCa")]
        public async Task<IActionResult> CreateTiLeTangCa([FromBody] TiLeTangCa model)
        {
            if (model == null || string.IsNullOrEmpty(model.TenTiLeTangCa))
            {
                return BadRequest("Invalid data");
            }

            model.KichHoat = false;
            _context.TiLeTangCas.Add(model);
            await _context.SaveChangesAsync();

            return Json(model);
        }

        [HttpPut("UpdateTiLeTangCa/{id}")]
        public async Task<IActionResult> UpdateTiLeTangCa(int id, [FromBody] TiLeTangCa model)
        {
            if (model == null || id != model.Id)
            {
                return BadRequest("Invalid data");
            }

            var existing = await _context.TiLeTangCas.FindAsync(id);
            if (existing == null)
            {
                return NotFound("Overtime rate not found");
            }

            existing.TenTiLeTangCa = model.TenTiLeTangCa;
            existing.TiLe = model.TiLe;
            existing.KichHoat = model.KichHoat;
            _context.TiLeTangCas.Update(existing);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("ActivateTiLeTangCa/{id}")]
        public async Task<IActionResult> ActivateTiLeTangCa(int id)
        {
            var rate = await _context.TiLeTangCas.FindAsync(id);
            if (rate == null)
            {
                return NotFound("Overtime rate not found");
            }

            rate.KichHoat = !rate.KichHoat;
            _context.TiLeTangCas.Update(rate);
            await _context.SaveChangesAsync();

            return Json(_context.TiLeTangCas.ToList());
        }

        [HttpPost("SaveGioChuan")]
        public async Task<IActionResult> SaveGioChuan([FromBody] GioChuan model)
        {
            if (model == null || model.Nam <= 0)
            {
                return BadRequest("Invalid data");
            }

            var existing = await _context.GioChuans
                .FirstOrDefaultAsync(x => x.Nam == model.Nam && x.IdLichLamViec == model.IdLichLamViec);

            var activeLichLamViec = await _context.LichLamViecs
                .FirstOrDefaultAsync(x => x.KichHoat);
            if (activeLichLamViec == null || !activeLichLamViec.KichHoat)
            {
                return BadRequest("No active work schedule found.");
            }

            if (existing != null)
            {
                existing.Thang1 = model.Thang1;
                existing.Thang2 = model.Thang2;
                existing.Thang3 = model.Thang3;
                existing.Thang4 = model.Thang4;
                existing.Thang5 = model.Thang5;
                existing.Thang6 = model.Thang6;
                existing.Thang7 = model.Thang7;
                existing.Thang8 = model.Thang8;
                existing.Thang9 = model.Thang9;
                existing.Thang10 = model.Thang10;
                existing.Thang11 = model.Thang11;
                existing.Thang12 = model.Thang12;
                existing.KichHoat = true;
                _context.GioChuans.Update(existing);
            }
            else
            {
                if (activeLichLamViec.KichHoat)
                {
                    model.IdLichLamViec = activeLichLamViec.Id;
                }
                else
                {
                    return BadRequest("The work schedule is not active.");
                }
                model.KichHoat = true;
                _context.GioChuans.Add(model);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetGioChuan/{year}")]
        public async Task<IActionResult> GetGioChuan(int year)
        {
            var data = await _context.GioChuans
                .Where(x => x.Nam == year && x.KichHoat)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
            return Json(data);
        }

        [HttpGet("GetAttendanceManagerRecords")]
        public IActionResult GetAttendanceRecords(int maNv)
        {
            var records = _context.LichSuChamCongs
                .Where(cc => cc.MaNv == maNv && (cc.TrangThai == null || cc.TrangThai == "Đã Duyệt"))
                .Select(cc => new
                {
                    cc.MaLichSuChamCong,
                    cc.Ngay,
                    cc.GioVao,
                    cc.GioRa,
                    cc.TongGio,
                    TrangThai = cc.TrangThai ?? "Đã Duyệt",
                    cc.GhiChu
                })
                .ToList();

            return Ok(new { success = true, records });
        }

        // New Endpoints for NgayLeCoDinh
        [HttpGet("GetNgayLeCoDinh")]
        public IActionResult GetNgayLeCoDinh()
        {
            var data = _context.NgayLeCoDinhs
                .OrderByDescending(x => x.MaNgayLe)
                .Select(x => new
                {
                    x.MaNgayLe,
                    x.TenNgayLe,
                    NgayLe1 = x.NgayLe1.ToString("yyyy-MM-dd"),
                    x.SoNgayNghi,
                    x.MoTa
                })
                .ToList();
            return Json(data);
        }

        [HttpPost("CreateNgayLeCoDinh")]
        public async Task<IActionResult> CreateNgayLeCoDinh([FromBody] NgayLeCoDinhDto model)
        {
            if (model == null || string.IsNullOrEmpty(model.TenNgayLe) || !DateOnly.TryParse(model.NgayLe1, out var ngayLe))
            {
                return BadRequest("Invalid data or date format");
            }

            try
            {
                var newRecord = new NgayLeCoDinh
                {
                    TenNgayLe = model.TenNgayLe,
                    NgayLe1 = ngayLe,
                    SoNgayNghi = model.SoNgayNghi,
                    MoTa = model.MoTa
                };
                _context.NgayLeCoDinhs.Add(newRecord);

                await _context.SaveChangesAsync();
                return Json(newRecord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("UpdateNgayLeCoDinh/{id}")]
        public async Task<IActionResult> UpdateNgayLeCoDinh(int id, [FromBody] NgayLeCoDinhDto model)
        {
            if (model == null || id != model.MaNgayLe || !DateOnly.TryParse(model.NgayLe1, out var ngayLe))
            {
                return BadRequest("Invalid data or date format");
            }

            var existing = await _context.NgayLeCoDinhs.FindAsync(id);
            if (existing == null)
            {
                return NotFound("Fixed holiday not found");
            }

            try
            {
                existing.TenNgayLe = model.TenNgayLe;
                existing.NgayLe1 = ngayLe;
                existing.SoNgayNghi = model.SoNgayNghi;
                existing.MoTa = model.MoTa;
                _context.NgayLeCoDinhs.Update(existing);

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("DeleteNgayLeCoDinh/{id}")]
        public async Task<IActionResult> DeleteNgayLeCoDinh(int id)
        {
            var holiday = await _context.NgayLeCoDinhs.FindAsync(id);
            if (holiday == null)
            {
                return NotFound("Fixed holiday not found");
            }

            try
            {
                _context.NgayLeCoDinhs.Remove(holiday);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetNgayLeCoDinh/{id}")]
        public async Task<IActionResult> GetNgayLeCoDinh(int id)
        {
            var holiday = await _context.NgayLeCoDinhs.FindAsync(id);
            if (holiday == null)
            {
                return NotFound("Fixed holiday not found");
            }

            var data = new
            {
                holiday.MaNgayLe,
                holiday.TenNgayLe,
                NgayLe1 = holiday.NgayLe1.ToString("yyyy-MM-dd"),
                holiday.SoNgayNghi,
                holiday.MoTa
            };
            return Json(data);
        }
    }

    // DTO to handle JSON deserialization
    public class ChamCongGioRaVaoDto
    {
        public string GioVao { get; set; }
        public string GioRa { get; set; }
        public bool KichHoat { get; set; }
        public decimal TongGio { get; set; }
    }

    // DTO for NgayLeCoDinh
    public class NgayLeCoDinhDto
    {
        public int MaNgayLe { get; set; }
        public string TenNgayLe { get; set; }
        public string NgayLe1 { get; set; }
        public int? SoNgayNghi { get; set; }
        public string? MoTa { get; set; }
    }
}