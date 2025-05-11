using Microsoft.AspNetCore.Mvc;
using HR_KD.Data;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.ApiControllers
{
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
            var data = _context.ChamCongGioRaVaos.OrderByDescending(x => x.Id).ToList();
            return Json(data);
        }

        [HttpPost("SetChamCongGioRaVao")]
        public async Task<IActionResult> SetChamCongGioRaVao([FromBody] ChamCongGioRaVao model)
        {
            if (model == null)
            {
                return BadRequest("Invalid data");
            }

            // Look for the most recent active record
            var existing = await _context.ChamCongGioRaVaos
                .Where(x => x.KichHoat)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Update the existing record
                existing.GioVao = model.GioVao;
                existing.GioRa = model.GioRa;
                existing.KichHoat = model.KichHoat;
                existing.TongGio = model.TongGio;
                _context.ChamCongGioRaVaos.Update(existing);
            }
            else
            {
                // Create a new record (do not set Id manually)
                var newRecord = new ChamCongGioRaVao
                {
                    GioVao = model.GioVao,
                    GioRa = model.GioRa,
                    KichHoat = model.KichHoat,
                    TongGio = model.TongGio
                };
                _context.ChamCongGioRaVaos.Add(newRecord);
            }

            await _context.SaveChangesAsync();
            return Ok();
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

            model.KichHoat = false; // Set default value
            _context.TiLeTangCas.Add(model); // Let EF Core generate the Id
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

            // Toggle the activation status for the selected rate
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

            // Check if a record exists for the given year and IdLichLamViec
            var existing = await _context.GioChuans
                .FirstOrDefaultAsync(x => x.Nam == model.Nam && x.IdLichLamViec == model.IdLichLamViec);

            // Get the active LichLamViec
            var activeLichLamViec = await _context.LichLamViecs
                .FirstOrDefaultAsync(x => x.KichHoat);
            if (activeLichLamViec == null || !activeLichLamViec.KichHoat)
            {
                return BadRequest("No active work schedule found.");
            }

            if (existing != null)
            {
                // Update existing record
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
                existing.KichHoat = true; // Activate the updated record
                _context.GioChuans.Update(existing);
            }
            else
            {
                // Create new record with the active LichLamViec ID
                if (activeLichLamViec.KichHoat) // Explicit check for KichHoat = true
                {
                    model.IdLichLamViec = activeLichLamViec.Id;
                }
                else
                {
                    return BadRequest("The work schedule is not active.");
                }
                model.KichHoat = true;
                _context.GioChuans.Add(model); // Let EF Core generate the Id
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
        // 🔹 Lấy danh sách chấm công của nhân viên
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
    }
}