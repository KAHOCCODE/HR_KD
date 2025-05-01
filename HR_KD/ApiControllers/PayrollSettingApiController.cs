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
            if (mucLuong.IsActive)
            {
                var activeRegions = await _context.MucLuongToiThieuVungs
                    .Where(m => m.IsActive)
                    .ToListAsync();
                foreach (var region in activeRegions)
                {
                    region.IsActive = false;
                    _context.Entry(region).State = EntityState.Modified;
                }
            }

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

            if (mucLuong.IsActive)
            {
                var activeRegions = await _context.MucLuongToiThieuVungs
                    .Where(m => m.IsActive && m.VungLuong != id)
                    .ToListAsync();
                foreach (var region in activeRegions)
                {
                    region.IsActive = false;
                    _context.Entry(region).State = EntityState.Modified;
                }
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

        // --- MucLuongCoSo Endpoints ---

        [HttpGet("muc-luong-co-so")]
        public async Task<ActionResult<IEnumerable<MucLuongCoSo>>> GetMucLuongCoSos()
        {
            return await _context.MucLuongCoSos.ToListAsync();
        }

        [HttpGet("muc-luong-co-so/{id}")]
        public async Task<ActionResult<MucLuongCoSo>> GetMucLuongCoSo(int id)
        {
            var mucLuongCoSo = await _context.MucLuongCoSos.FindAsync(id);
            if (mucLuongCoSo == null)
            {
                return NotFound();
            }
            return mucLuongCoSo;
        }

        [HttpPost("muc-luong-co-so")]
        public async Task<ActionResult<MucLuongCoSo>> CreateMucLuongCoSo(MucLuongCoSo mucLuongCoSo)
        {
            var allMucLuongCoSo = await _context.MucLuongCoSos.ToListAsync();
            foreach (var existing in allMucLuongCoSo)
            {
                existing.IsActive = false;
                _context.Entry(existing).State = EntityState.Modified;
            }

            mucLuongCoSo.IsActive = true;

            var previousMucLuongCoSo = allMucLuongCoSo
                .Where(m => m.NgayHetHieuLuc == null && m.Id != mucLuongCoSo.Id)
                .OrderByDescending(m => m.NgayHieuLuc)
                .FirstOrDefault();

            _context.MucLuongCoSos.Add(mucLuongCoSo);
            await _context.SaveChangesAsync();

            if (previousMucLuongCoSo != null)
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

            var allMucLuongCoSo = await _context.MucLuongCoSos
                .Where(m => m.Id != id)
                .ToListAsync();
            foreach (var existing in allMucLuongCoSo)
            {
                existing.IsActive = false;
                _context.Entry(existing).State = EntityState.Modified;
            }

            mucLuongCoSo.IsActive = true;

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
            return await _context.ThongTinBaoHiems.ToListAsync();
        }

        [HttpGet("thong-tin-bao-hiem/{id}")]
        public async Task<ActionResult<ThongTinBaoHiem>> GetThongTinBaoHiem(int id)
        {
            var thongTinBaoHiem = await _context.ThongTinBaoHiems
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
            // Kiểm tra dữ liệu đầu vào
            if (createDto.TyLeNguoiLaoDongBHXH < 0 || createDto.TyLeNhaTuyenDungBHXH < 0 ||
                createDto.TyLeNguoiLaoDongBHYT < 0 || createDto.TyLeNhaTuyenDungBHYT < 0 ||
                createDto.TyLeNguoiLaoDongBHTN < 0 || createDto.TyLeNhaTuyenDungBHTN < 0)
            {
                return BadRequest("Tỷ lệ không được âm.");
            }

            if (createDto.NgayHieuLuc == default(DateTime))
            {
                return BadRequest("Ngày hiệu lực không hợp lệ.");
            }

            if (createDto.NgayHetHieuLuc.HasValue && createDto.NgayHetHieuLuc <= createDto.NgayHieuLuc)
            {
                return BadRequest("Ngày hết hiệu lực phải lớn hơn ngày hiệu lực.");
            }

            var insuranceTypes = new[] { "BHXH", "BHYT", "BHTN" };
            var createdRecords = new List<ThongTinBaoHiem>();

            foreach (var type in insuranceTypes)
            {
                var previousRecord = await _context.ThongTinBaoHiems
                    .Where(t => t.LoaiBaoHiem == type && t.NgayHetHieuLuc == null)
                    .OrderByDescending(t => t.NgayHieuLuc)
                    .FirstOrDefaultAsync();

                var thongTinBaoHiem = new ThongTinBaoHiem
                {
                    LoaiBaoHiem = type,
                    TyLeNguoiLaoDong = type == "BHXH" ? createDto.TyLeNguoiLaoDongBHXH :
                                       type == "BHYT" ? createDto.TyLeNguoiLaoDongBHYT :
                                       createDto.TyLeNguoiLaoDongBHTN,
                    TyLeNhaTuyenDung = type == "BHXH" ? createDto.TyLeNhaTuyenDungBHXH :
                                        type == "BHYT" ? createDto.TyLeNhaTuyenDungBHYT :
                                        createDto.TyLeNhaTuyenDungBHTN,
                    NgayHieuLuc = createDto.NgayHieuLuc,
                    NgayHetHieuLuc = createDto.NgayHetHieuLuc,
                    GhiChu = createDto.GhiChu
                };

                _context.ThongTinBaoHiems.Add(thongTinBaoHiem);
                createdRecords.Add(thongTinBaoHiem);

                if (previousRecord != null)
                {
                    previousRecord.NgayHetHieuLuc = createDto.NgayHieuLuc;
                    _context.Entry(previousRecord).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetThongTinBaoHiem), new { id = createdRecords.First().Id }, createdRecords);
        }

        [HttpPut("thong-tin-bao-hiem/{id}")]
        public async Task<IActionResult> UpdateThongTinBaoHiem(int id, ThongTinBaoHiem thongTinBaoHiem)
        {
            if (id != thongTinBaoHiem.Id)
            {
                return BadRequest("ID không khớp.");
            }

            var existingRecord = await _context.ThongTinBaoHiems
                .FirstOrDefaultAsync(t => t.Id == id && t.NgayHetHieuLuc == null);
            if (existingRecord == null)
            {
                return BadRequest("Chỉ có thể chỉnh sửa bản ghi mới nhất (chưa có ngày hết hiệu lực).");
            }

            if (thongTinBaoHiem.TyLeNguoiLaoDong < 0 || thongTinBaoHiem.TyLeNhaTuyenDung < 0)
            {
                return BadRequest("Tỷ lệ không được âm.");
            }

            if (thongTinBaoHiem.NgayHieuLuc == default(DateTime))
            {
                return BadRequest("Ngày hiệu lực không hợp lệ.");
            }

            if (thongTinBaoHiem.NgayHetHieuLuc.HasValue && thongTinBaoHiem.NgayHetHieuLuc <= thongTinBaoHiem.NgayHieuLuc)
            {
                return BadRequest("Ngày hết hiệu lực phải lớn hơn ngày hiệu lực.");
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
            if (cauHinh.MucLuongTu < 0 || cauHinh.MucLuongDen < 0 || cauHinh.GiaTri < 0)
            {
                return BadRequest("Các giá trị không được âm.");
            }

            if (cauHinh.MucLuongDen.HasValue && cauHinh.MucLuongDen <= cauHinh.MucLuongTu)
            {
                return BadRequest("Mức lương đến phải lớn hơn mức lương từ.");
            }

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

            if (cauHinh.MucLuongTu < 0 || cauHinh.MucLuongDen < 0 || cauHinh.GiaTri < 0)
            {
                return BadRequest("Các giá trị không được âm.");
            }

            if (cauHinh.MucLuongDen.HasValue && cauHinh.MucLuongDen <= cauHinh.MucLuongTu)
            {
                return BadRequest("Mức lương đến phải lớn hơn mức lương từ.");
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

        // --- GiamTruGiaCanh Endpoints ---

        [HttpGet("giam-tru-gia-canh")]
        public async Task<ActionResult<IEnumerable<GiamTruGiaCanh>>> GetGiamTruGiaCanhs()
        {
            return await _context.GiamTruGiaCanhs.ToListAsync();
        }

        [HttpGet("giam-tru-gia-canh/{id}")]
        public async Task<ActionResult<GiamTruGiaCanh>> GetGiamTruGiaCanh(int id)
        {
            var giamTruGiaCanh = await _context.GiamTruGiaCanhs.FindAsync(id);
            if (giamTruGiaCanh == null)
            {
                return NotFound();
            }
            return giamTruGiaCanh;
        }

        [HttpPost("giam-tru-gia-canh")]
        public async Task<ActionResult<GiamTruGiaCanh>> CreateGiamTruGiaCanh(GiamTruGiaCanh giamTruGiaCanh)
        {
            if (giamTruGiaCanh.MucGiamTruBanThan < 0 || giamTruGiaCanh.MucGiamTruNguoiPhuThuoc < 0)
            {
                return BadRequest("Mức giảm trừ không được âm.");
            }

            if (giamTruGiaCanh.NgayHieuLuc == default(DateTime))
            {
                return BadRequest("Ngày hiệu lực không hợp lệ.");
            }

            if (giamTruGiaCanh.NgayHetHieuLuc.HasValue && giamTruGiaCanh.NgayHetHieuLuc <= giamTruGiaCanh.NgayHieuLuc)
            {
                return BadRequest("Ngày hết hiệu lực phải lớn hơn ngày hiệu lực.");
            }

            var previousRecord = await _context.GiamTruGiaCanhs
                .Where(g => g.NgayHetHieuLuc == null)
                .OrderByDescending(g => g.NgayHieuLuc)
                .FirstOrDefaultAsync();

            _context.GiamTruGiaCanhs.Add(giamTruGiaCanh);
            await _context.SaveChangesAsync();

            if (previousRecord != null)
            {
                previousRecord.NgayHetHieuLuc = giamTruGiaCanh.NgayHieuLuc;
                _context.Entry(previousRecord).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetGiamTruGiaCanh), new { id = giamTruGiaCanh.Id }, giamTruGiaCanh);
        }

        [HttpPut("giam-tru-gia-canh/{id}")]
        public async Task<IActionResult> UpdateGiamTruGiaCanh(int id, GiamTruGiaCanh giamTruGiaCanh)
        {
            if (id != giamTruGiaCanh.Id)
            {
                return BadRequest("ID không khớp.");
            }

            var existingRecord = await _context.GiamTruGiaCanhs
                .FirstOrDefaultAsync(g => g.Id == id && g.NgayHetHieuLuc == null);
            if (existingRecord == null)
            {
                return BadRequest("Chỉ có thể chỉnh sửa bản ghi mới nhất (chưa có ngày hết hiệu lực).");
            }

            if (giamTruGiaCanh.MucGiamTruBanThan < 0 || giamTruGiaCanh.MucGiamTruNguoiPhuThuoc < 0)
            {
                return BadRequest("Mức giảm trừ không được âm.");
            }

            if (giamTruGiaCanh.NgayHieuLuc == default(DateTime))
            {
                return BadRequest("Ngày hiệu lực không hợp lệ.");
            }

            if (giamTruGiaCanh.NgayHetHieuLuc.HasValue && giamTruGiaCanh.NgayHetHieuLuc <= giamTruGiaCanh.NgayHieuLuc)
            {
                return BadRequest("Ngày hết hiệu lực phải lớn hơn ngày hiệu lực.");
            }

            _context.Entry(giamTruGiaCanh).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.GiamTruGiaCanhs.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return NoContent();
        }

        [HttpDelete("giam-tru-gia-canh/{id}")]
        public async Task<IActionResult> DeleteGiamTruGiaCanh(int id)
        {
            var giamTruGiaCanh = await _context.GiamTruGiaCanhs.FindAsync(id);
            if (giamTruGiaCanh == null)
            {
                return NotFound();
            }

            _context.GiamTruGiaCanhs.Remove(giamTruGiaCanh);
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
        public DateTime NgayHieuLuc { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public string GhiChu { get; set; }
    }

    public class ThongTinBaoHiemDto
    {
        public int Id { get; set; }
        public string LoaiBaoHiem { get; set; }
        public decimal TyLeNguoiLaoDong { get; set; }
        public decimal TyLeNhaTuyenDung { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public string GhiChu { get; set; }
    }
}