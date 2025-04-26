using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using HR_KD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HR_KD.DTOs;

namespace HR_KD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollSettingApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public PayrollSettingApiController(HrDbContext context)
        {
            _context = context;
        }

        // --- MucLuongToiThieuVung Endpoints ---

        [HttpGet("muc-luong-toi-thieu-vung")]
        public async Task<ActionResult<IEnumerable<MucLuongToiThieuVung>>> GetMucLuongToiThieuVungs()
        {
            return await _context.MucLuongToiThieuVungs.ToListAsync();
        }

        [HttpGet("muc-luong-toi-thieu-vung/{id}")]
        public async Task<ActionResult<MucLuongToiThieuVung>> GetMucLuongToiThieuVung(int id)
        {
            var mucLuong = await _context.MucLuongToiThieuVungs.FindAsync(id);
            if (mucLuong == null)
            {
                return NotFound();
            }
            return mucLuong;
        }

        [HttpPost("muc-luong-toi-thieu-vung")]
        public async Task<ActionResult<MucLuongToiThieuVung>> CreateMucLuongToiThieuVung(MucLuongToiThieuVung mucLuong)
        {
            _context.MucLuongToiThieuVungs.Add(mucLuong);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetMucLuongToiThieuVung), new { id = mucLuong.VungLuong }, mucLuong);
        }

        [HttpPut("muc-luong-toi-thieu-vung/{id}")]
        public async Task<IActionResult> UpdateMucLuongToiThieuVung(int id, MucLuongToiThieuVung mucLuong)
        {
            if (id != mucLuong.VungLuong)
            {
                return BadRequest();
            }

            _context.Entry(mucLuong).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.MucLuongToiThieuVungs.Any(e => e.VungLuong == id))
                {
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        // --- VungLuongTheoDiaPhuong Endpoints ---

        [HttpGet("vung-luong-theo-dia-phuong")]
        public async Task<ActionResult<IEnumerable<VungLuongTheoDiaPhuong>>> GetVungLuongTheoDiaPhuongs()
        {
            return await _context.VungLuongTheoDiaPhuongs
                .Include(v => v.MucLuongVung)
                .ToListAsync();
        }

        [HttpGet("vung-luong-theo-dia-phuong/{id}")]
        public async Task<ActionResult<VungLuongTheoDiaPhuong>> GetVungLuongTheoDiaPhuong(int id)
        {
            var vungLuong = await _context.VungLuongTheoDiaPhuongs
                .Include(v => v.MucLuongVung)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (vungLuong == null)
            {
                return NotFound();
            }
            return vungLuong;
        }

        [HttpGet("tinh-thanh")]
        public async Task<ActionResult<IEnumerable<string>>> GetTinhThanhs()
        {
            var tinhThanhs = await _context.VungLuongTheoDiaPhuongs
                .Select(v => v.TinhThanh)
                .Distinct()
                .ToListAsync();
            return tinhThanhs;
        }

        [HttpGet("quan-huyen")]
        public async Task<ActionResult<IEnumerable<string>>> GetQuanHuyens([FromQuery] string tinhThanh)
        {
            if (string.IsNullOrEmpty(tinhThanh))
            {
                return BadRequest("TinhThanh is required.");
            }

            var quanHuyens = await _context.VungLuongTheoDiaPhuongs
                .Where(v => v.TinhThanh == tinhThanh)
                .Select(v => v.QuanHuyen)
                .Distinct()
                .ToListAsync();
            return quanHuyens;
        }

        [HttpGet("vung-luong-theo-dia-phuong/by-location")]
        public async Task<ActionResult<VungLuongTheoDiaPhuong>> GetVungLuongByLocation([FromQuery] string tinhThanh, [FromQuery] string quanHuyen)
        {
            if (string.IsNullOrEmpty(tinhThanh) || string.IsNullOrEmpty(quanHuyen))
            {
                return BadRequest("TinhThanh and QuanHuyen are required.");
            }

            var vungLuong = await _context.VungLuongTheoDiaPhuongs
                .Include(v => v.MucLuongVung)
                .FirstOrDefaultAsync(v => v.TinhThanh == tinhThanh && v.QuanHuyen == quanHuyen);
            if (vungLuong == null)
            {
                return NotFound();
            }
            return vungLuong;
        }

        [HttpPost("vung-luong-theo-dia-phuong")]
        public async Task<ActionResult<VungLuongTheoDiaPhuong>> CreateVungLuongTheoDiaPhuong(VungLuongTheoDiaPhuong vungLuong)
        {
            _context.VungLuongTheoDiaPhuongs.Add(vungLuong);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetVungLuongTheoDiaPhuong), new { id = vungLuong.Id }, vungLuong);
        }

        [HttpPut("vung-luong-theo-dia-phuong/{id}")]
        public async Task<IActionResult> UpdateVungLuongTheoDiaPhuong(int id, [FromBody] UpdateVungLuongDto updateData)
        {
            if (id != updateData.Id)
            {
                return BadRequest("ID in URL does not match ID in request body.");
            }

            var vungLuong = await _context.VungLuongTheoDiaPhuongs.FindAsync(id);
            if (vungLuong == null)
            {
                return NotFound();
            }

            // If setting IsActive to true, deactivate all other regions
            if (updateData.IsActive)
            {
                var otherRegions = await _context.VungLuongTheoDiaPhuongs
                    .Where(v => v.Id != id && v.IsActive)
                    .ToListAsync();
                foreach (var region in otherRegions)
                {
                    region.IsActive = false;
                }
            }

            vungLuong.IsActive = updateData.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.VungLuongTheoDiaPhuongs.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        // --- MucLuongCoSo Endpoints ---

        [HttpGet("muc-luong-co-so")]
        public async Task<ActionResult<IEnumerable<MucLuongCoSo>>> GetMucLuongCoSos()
        {
            return await _context.MucLuongCoSos.ToListAsync();
        }

        [HttpGet("muc-luong-co-so/{id}")]
        public async Task<ActionResult<MucLuongCoSo>> GetMucLuongCoSo(int id)
        {
            var muclươngCoSo = await _context.MucLuongCoSos.FindAsync(id);
            if (muclươngCoSo == null)
            {
                return NotFound();
            }
            return muclươngCoSo;
        }

        [HttpPost("muc-luong-co-so")]
        public async Task<ActionResult<MucLuongCoSo>> CreateMucLuongCoSo(MucLuongCoSo mucLuongCoSo)
        {
            // Find the latest MucLuongCoSo without NgayHetHieuLuc
            var previousMucLuongCoSo = await _context.MucLuongCoSos
                .Where(m => m.NgayHetHieuLuc == null)
                .OrderByDescending(m => m.NgayHieuLuc)
                .FirstOrDefaultAsync();

            _context.MucLuongCoSos.Add(mucLuongCoSo);
            await _context.SaveChangesAsync();

            // Update the previous record's NgayHetHieuLuc if it exists
            if (previousMucLuongCoSo != null && previousMucLuongCoSo.Id != mucLuongCoSo.Id)
            {
                previousMucLuongCoSo.NgayHetHieuLuc = mucLuongCoSo.NgayHieuLuc;
                _context.Entry(previousMucLuongCoSo).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetMucLuongCoSo), new { id = mucLuongCoSo.Id }, mucLuongCoSo);
        }

        [HttpPut("muc-luong-co-so/{id}")]
        public async Task<IActionResult> UpdateMucLuongCoSo(int id, MucLuongCoSo mucLuongCoSo)
        {
            if (id != mucLuongCoSo.Id)
            {
                return BadRequest();
            }

            _context.Entry(mucLuongCoSo).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.MucLuongCoSos.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        // --- ThongTinBaoHiem Endpoints ---

        [HttpGet("thong-tin-bao-hiem")]
        public async Task<ActionResult<IEnumerable<ThongTinBaoHiem>>> GetThongTinBaoHiems()
        {
            return await _context.ThongTinBaoHiems
                .Include(t => t.VungLuongTheoDiaPhuong)
                .ThenInclude(v => v.MucLuongVung)
                .Include(t => t.MucLuongCoSo)
                .ToListAsync();
        }

        [HttpGet("thong-tin-bao-hiem/{id}")]
        public async Task<ActionResult<ThongTinBaoHiem>> GetThongTinBaoHiem(int id)
        {
            var thongTinBaoHiem = await _context.ThongTinBaoHiems
                .Include(t => t.VungLuongTheoDiaPhuong)
                .ThenInclude(v => v.MucLuongVung)
                .Include(t => t.MucLuongCoSo)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (thongTinBaoHiem == null)
            {
                return NotFound();
            }
            return thongTinBaoHiem;
        }

        [HttpPost("thong-tin-bao-hiem")]
        public async Task<ActionResult<IEnumerable<ThongTinBaoHiem>>> CreateThongTinBaoHiem([FromBody] CreateThongTinBaoHiemDto createDto)
        {
            var insuranceTypes = new[] { "BHXH", "BHYT", "BHTN" };
            var createdRecords = new List<ThongTinBaoHiem>();

            foreach (var type in insuranceTypes)
            {
                var thongTinBaoHiem = new ThongTinBaoHiem
                {
                    LoaiBaoHiem = type,
                    TyLeNguoiLaoDong = type == "BHXH" ? createDto.TyLeNguoiLaoDongBHXH :
                                       type == "BHYT" ? createDto.TyLeNguoiLaoDongBHYT :
                                       createDto.TyLeNguoiLaoDongBHTN,
                    TyLeNhaTuyenDung = type == "BHXH" ? createDto.TyLeNhaTuyenDungBHXH :
                                        type == "BHYT" ? createDto.TyLeNhaTuyenDungBHYT :
                                        createDto.TyLeNhaTuyenDungBHTN,
                    VungLuongTheoDiaPhuongId = createDto.VungLuongTheoDiaPhuongId,
                    MucLuongCoSoId = createDto.MucLuongCoSoId,
                    NgayHieuLuc = createDto.NgayHieuLuc,
                    NgayHetHieuLuc = createDto.NgayHetHieuLuc,
                    GhiChu = createDto.GhiChu
                };

                _context.ThongTinBaoHiems.Add(thongTinBaoHiem);
                createdRecords.Add(thongTinBaoHiem);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetThongTinBaoHiem), new { id = createdRecords.First().Id }, createdRecords);
        }

        [HttpPut("thong-tin-bao-hiem/{id}")]
        public async Task<IActionResult> UpdateThongTinBaoHiem(int id, ThongTinBaoHiem thongTinBaoHiem)
        {
            if (id != thongTinBaoHiem.Id)
            {
                return BadRequest();
            }

            _context.Entry(thongTinBaoHiem).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ThongTinBaoHiems.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        // --- CauHinhLuongThue Endpoints ---

        [HttpGet("cau-hinh-luong-thue")]
        public async Task<ActionResult<IEnumerable<CauHinhLuongThue>>> GetCauHinhLuongThues()
        {
            return await _context.CauHinhLuongThues.ToListAsync();
        }

        [HttpGet("cau-hinh-luong-thue/{id}")]
        public async Task<ActionResult<CauHinhLuongThue>> GetCauHinhLuongThue(int id)
        {
            var cauHinh = await _context.CauHinhLuongThues.FindAsync(id);
            if (cauHinh == null)
            {
                return NotFound();
            }
            return cauHinh;
        }

        [HttpPost("cau-hinh-luong-thue")]
        public async Task<ActionResult<CauHinhLuongThue>> CreateCauHinhLuongThue(CauHinhLuongThue cauHinh)
        {
            _context.CauHinhLuongThues.Add(cauHinh);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCauHinhLuongThue), new { id = cauHinh.MaCauHinh }, cauHinh);
        }

        [HttpPut("cau-hinh-luong-thue/{id}")]
        public async Task<IActionResult> UpdateCauHinhLuongThue(int id, CauHinhLuongThue cauHinh)
        {
            if (id != cauHinh.MaCauHinh)
            {
                return BadRequest();
            }

            _context.Entry(cauHinh).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CauHinhLuongThues.Any(e => e.MaCauHinh == id))
                {
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        [HttpDelete("cau-hinh-luong-thue/{id}")]
        public async Task<IActionResult> DeleteCauHinhLuongThue(int id)
        {
            var cauHinh = await _context.CauHinhLuongThues.FindAsync(id);
            if (cauHinh == null)
            {
                return NotFound();
            }

            _context.CauHinhLuongThues.Remove(cauHinh);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CreateThongTinBaoHiemDto
    {
        public decimal TyLeNguoiLaoDongBHXH { get; set; }
        public decimal TyLeNhaTuyenDungBHXH { get; set; }
        public decimal TyLeNguoiLaoDongBHYT { get; set; }
        public decimal TyLeNhaTuyenDungBHYT { get; set; }
        public decimal TyLeNguoiLaoDongBHTN { get; set; }
        public decimal TyLeNhaTuyenDungBHTN { get; set; }
        public int VungLuongTheoDiaPhuongId { get; set; }
        public int MucLuongCoSoId { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public string GhiChu { get; set; }
    }

    public class UpdateVungLuongDto
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }
}