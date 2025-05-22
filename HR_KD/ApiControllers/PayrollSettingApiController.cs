using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using HR_KD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace HR_KD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "DIRECTOR")]
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
        public async Task<ActionResult<IEnumerable<ThongTinBaoHiemDto>>> GetThongTinBaoHiems()
        {
            var records = await _context.ThongTinBaoHiems
                .Select(t => new ThongTinBaoHiemDto
                {
                    Id = t.Id,
                    LoaiBaoHiem = t.LoaiBaoHiem,
                    TyLeNguoiLaoDong = t.TyLeNguoiLaoDong,
                    TyLeNhaTuyenDung = t.TyLeNhaTuyenDung,
                    NgayHieuLuc = t.NgayHieuLuc,
                    NgayHetHieuLuc = t.NgayHetHieuLuc,
                    GhiChu = t.GhiChu
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("thong-tin-bao-hiem/{id}")]
        public async Task<ActionResult<ThongTinBaoHiemDto>> GetThongTinBaoHiem(int id)
        {
            var record = await _context.ThongTinBaoHiems
                .Where(t => t.Id == id)
                .Select(t => new ThongTinBaoHiemDto
                {
                    Id = t.Id,
                    LoaiBaoHiem = t.LoaiBaoHiem,
                    TyLeNguoiLaoDong = t.TyLeNguoiLaoDong,
                    TyLeNhaTuyenDung = t.TyLeNhaTuyenDung,
                    NgayHieuLuc = t.NgayHieuLuc,
                    NgayHetHieuLuc = t.NgayHetHieuLuc,
                    GhiChu = t.GhiChu
                })
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi bảo hiểm với ID {id}." });
            }

            return Ok(record);
        }

        [HttpPost("thong-tin-bao-hiem")]
        public async Task<ActionResult<IEnumerable<ThongTinBaoHiemDto>>> CreateThongTinBaoHiem([FromBody] CreateThongTinBaoHiemDto dto)
        {
            // Validate input
            if (dto.TyLeNguoiLaoDongBHXH < 0 || dto.TyLeNhaTuyenDungBHXH < 0 ||
                dto.TyLeNguoiLaoDongBHYT < 0 || dto.TyLeNhaTuyenDungBHYT < 0 ||
                dto.TyLeNguoiLaoDongBHTN < 0 || dto.TyLeNhaTuyenDungBHTN < 0)
            {
                return BadRequest(new { message = "Tỷ lệ không được âm." });
            }

            if (dto.NgayHieuLuc == default)
            {
                return BadRequest(new { message = "Ngày hiệu lực không hợp lệ." });
            }

            if (dto.NgayHetHieuLuc.HasValue && dto.NgayHetHieuLuc <= dto.NgayHieuLuc)
            {
                return BadRequest(new { message = "Ngày hết hiệu lực phải lớn hơn ngày hiệu lực." });
            }

            var insuranceTypes = new[] { "BHXH", "BHYT", "BHTN" };
            var createdRecords = new List<ThongTinBaoHiem>();

            foreach (var type in insuranceTypes)
            {
                // Check for overlapping date ranges
                var overlappingRecord = await _context.ThongTinBaoHiems
                    .FirstOrDefaultAsync(t => t.LoaiBaoHiem == type &&
                        t.NgayHieuLuc <= dto.NgayHieuLuc &&
                        (t.NgayHetHieuLuc == null || t.NgayHetHieuLuc >= dto.NgayHieuLuc));

                if (overlappingRecord != null)
                {
                    return BadRequest(new { message = $"Ngày hiệu lực trùng với bản ghi hiện có cho {type}." });
                }

                // Update previous record
                var previousRecord = await _context.ThongTinBaoHiems
                    .Where(t => t.LoaiBaoHiem == type && t.NgayHetHieuLuc == null)
                    .OrderByDescending(t => t.NgayHieuLuc)
                    .FirstOrDefaultAsync();

                if (previousRecord != null)
                {
                    previousRecord.NgayHetHieuLuc = dto.NgayHieuLuc;
                    _context.Entry(previousRecord).State = EntityState.Modified;
                }

                // Create new record
                var newRecord = new ThongTinBaoHiem
                {
                    LoaiBaoHiem = type,
                    TyLeNguoiLaoDong = type == "BHXH" ? dto.TyLeNguoiLaoDongBHXH :
                                       type == "BHYT" ? dto.TyLeNguoiLaoDongBHYT :
                                       dto.TyLeNguoiLaoDongBHTN,
                    TyLeNhaTuyenDung = type == "BHXH" ? dto.TyLeNhaTuyenDungBHXH :
                                        type == "BHYT" ? dto.TyLeNhaTuyenDungBHYT :
                                        dto.TyLeNhaTuyenDungBHTN,
                    NgayHieuLuc = dto.NgayHieuLuc,
                    NgayHetHieuLuc = dto.NgayHetHieuLuc,
                    GhiChu = dto.GhiChu
                };

                _context.ThongTinBaoHiems.Add(newRecord);
                createdRecords.Add(newRecord);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi tạo bản ghi.", detail = ex.Message });
            }

            var responseDtos = createdRecords.Select(t => new ThongTinBaoHiemDto
            {
                Id = t.Id,
                LoaiBaoHiem = t.LoaiBaoHiem,
                TyLeNguoiLaoDong = t.TyLeNguoiLaoDong,
                TyLeNhaTuyenDung = t.TyLeNhaTuyenDung,
                NgayHieuLuc = t.NgayHieuLuc,
                NgayHetHieuLuc = t.NgayHetHieuLuc,
                GhiChu = t.GhiChu
            }).ToList();

            return CreatedAtAction(nameof(GetThongTinBaoHiem), new { id = responseDtos.First().Id }, responseDtos);
        }

        [HttpPut("thong-tin-bao-hiem/{id}")]
        public async Task<ActionResult<ThongTinBaoHiemDto>> UpdateThongTinBaoHiem(int id, [FromBody] ThongTinBaoHiemDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { message = "ID không khớp." });
            }

            if (!new[] { "BHXH", "BHYT", "BHTN" }.Contains(dto.LoaiBaoHiem))
            {
                return BadRequest(new { message = "Loại bảo hiểm không hợp lệ." });
            }

            var existingRecord = await _context.ThongTinBaoHiems
                .FirstOrDefaultAsync(t => t.Id == id && t.LoaiBaoHiem == dto.LoaiBaoHiem && t.NgayHetHieuLuc == null);

            if (existingRecord == null)
            {
                return BadRequest(new { message = "Chỉ có thể chỉnh sửa bản ghi mới nhất (chưa có ngày hết hiệu lực) của loại bảo hiểm này." });
            }

            // Validate input
            if (dto.TyLeNguoiLaoDong < 0 || dto.TyLeNhaTuyenDung < 0)
            {
                return BadRequest(new { message = "Tỷ lệ không được âm." });
            }

            if (dto.NgayHieuLuc == default)
            {
                return BadRequest(new { message = "Ngày hiệu lực không hợp lệ." });
            }

            if (dto.NgayHetHieuLuc.HasValue && dto.NgayHetHieuLuc <= dto.NgayHieuLuc)
            {
                return BadRequest(new { message = "Ngày hết hiệu lực phải lớn hơn ngày hiệu lực." });
            }

            // Check for overlapping date ranges
            var overlappingRecord = await _context.ThongTinBaoHiems
                .FirstOrDefaultAsync(t => t.LoaiBaoHiem == dto.LoaiBaoHiem && t.Id != id &&
                    t.NgayHieuLuc <= dto.NgayHieuLuc &&
                    (t.NgayHetHieuLuc == null || t.NgayHetHieuLuc >= dto.NgayHieuLuc));

            if (overlappingRecord != null)
            {
                return BadRequest(new { message = $"Ngày hiệu lực trùng với bản ghi hiện có cho {dto.LoaiBaoHiem}." });
            }

            // Update record
            existingRecord.TyLeNguoiLaoDong = dto.TyLeNguoiLaoDong;
            existingRecord.TyLeNhaTuyenDung = dto.TyLeNhaTuyenDung;
            existingRecord.NgayHieuLuc = dto.NgayHieuLuc;
            existingRecord.NgayHetHieuLuc = dto.NgayHetHieuLuc;
            existingRecord.GhiChu = dto.GhiChu;

            _context.Entry(existingRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.ThongTinBaoHiems.AnyAsync(t => t.Id == id))
                {
                    return NotFound(new { message = $"Không tìm thấy bản ghi bảo hiểm với ID {id}." });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi cập nhật bản ghi.", detail = ex.Message });
            }

            var responseDto = new ThongTinBaoHiemDto
            {
                Id = existingRecord.Id,
                LoaiBaoHiem = existingRecord.LoaiBaoHiem,
                TyLeNguoiLaoDong = existingRecord.TyLeNguoiLaoDong,
                TyLeNhaTuyenDung = existingRecord.TyLeNhaTuyenDung,
                NgayHieuLuc = existingRecord.NgayHieuLuc,
                NgayHetHieuLuc = existingRecord.NgayHetHieuLuc,
                GhiChu = existingRecord.GhiChu
            };

            return Ok(responseDto);
        }

        [HttpDelete("thong-tin-bao-hiem/{id}")]
        public async Task<IActionResult> DeleteThongTinBaoHiem(int id)
        {
            var record = await _context.ThongTinBaoHiems.FindAsync(id);
            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi bảo hiểm với ID {id}." });
            }

            _context.ThongTinBaoHiems.Remove(record);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi xóa bản ghi.", detail = ex.Message });
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
        public async Task<ActionResult<IEnumerable<GiamTruGiaCanhDto>>> GetGiamTruGiaCanhs()
        {
            var records = await _context.GiamTruGiaCanhs
                .Select(g => new GiamTruGiaCanhDto
                {
                    Id = g.Id,
                    MucGiamTruBanThan = g.MucGiamTruBanThan,
                    MucGiamTruNguoiPhuThuoc = g.MucGiamTruNguoiPhuThuoc,
                    NgayHieuLuc = g.NgayHieuLuc,
                    NgayHetHieuLuc = g.NgayHetHieuLuc,
                    GhiChu = g.GhiChu
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("giam-tru-gia-canh/{id}")]
        public async Task<ActionResult<GiamTruGiaCanhDto>> GetGiamTruGiaCanh(int id)
        {
            var record = await _context.GiamTruGiaCanhs
                .Where(g => g.Id == id)
                .Select(g => new GiamTruGiaCanhDto
                {
                    Id = g.Id,
                    MucGiamTruBanThan = g.MucGiamTruBanThan,
                    MucGiamTruNguoiPhuThuoc = g.MucGiamTruNguoiPhuThuoc,
                    NgayHieuLuc = g.NgayHieuLuc,
                    NgayHetHieuLuc = g.NgayHetHieuLuc,
                    GhiChu = g.GhiChu
                })
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi giảm trừ gia cảnh với ID {id}." });
            }

            return Ok(record);
        }

        [HttpPost("giam-tru-gia-canh")]
        public async Task<ActionResult<GiamTruGiaCanhDto>> CreateGiamTruGiaCanh([FromBody] GiamTruGiaCanhDto dto)
        {
            // Validate input
            if (dto.MucGiamTruBanThan < 0 || dto.MucGiamTruNguoiPhuThuoc < 0)
            {
                return BadRequest(new { message = "Mức giảm trừ không được âm." });
            }

            if (dto.NgayHieuLuc == default)
            {
                return BadRequest(new { message = "Ngày hiệu lực không hợp lệ." });
            }

            if (dto.NgayHetHieuLuc.HasValue && dto.NgayHetHieuLuc <= dto.NgayHieuLuc)
            {
                return BadRequest(new { message = "Ngày hết hiệu lực phải lớn hơn ngày hiệu lực." });
            }

            // Check for overlapping date ranges
            var overlappingRecord = await _context.GiamTruGiaCanhs
                .FirstOrDefaultAsync(g => g.NgayHieuLuc <= dto.NgayHieuLuc &&
                    (g.NgayHetHieuLuc == null || g.NgayHetHieuLuc >= dto.NgayHieuLuc));

            if (overlappingRecord != null)
            {
                return BadRequest(new { message = "Ngày hiệu lực trùng với bản ghi hiện có." });
            }

            // Update previous record
            var previousRecord = await _context.GiamTruGiaCanhs
                .Where(g => g.NgayHetHieuLuc == null)
                .OrderByDescending(g => g.NgayHieuLuc)
                .FirstOrDefaultAsync();

            if (previousRecord != null)
            {
                previousRecord.NgayHetHieuLuc = dto.NgayHieuLuc;
                _context.Entry(previousRecord).State = EntityState.Modified;
            }

            // Create new record
            var newRecord = new GiamTruGiaCanh
            {
                MucGiamTruBanThan = dto.MucGiamTruBanThan,
                MucGiamTruNguoiPhuThuoc = dto.MucGiamTruNguoiPhuThuoc,
                NgayHieuLuc = dto.NgayHieuLuc,
                NgayHetHieuLuc = dto.NgayHetHieuLuc,
                GhiChu = dto.GhiChu
            };

            _context.GiamTruGiaCanhs.Add(newRecord);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi tạo bản ghi.", detail = ex.Message });
            }

            var responseDto = new GiamTruGiaCanhDto
            {
                Id = newRecord.Id,
                MucGiamTruBanThan = newRecord.MucGiamTruBanThan,
                MucGiamTruNguoiPhuThuoc = newRecord.MucGiamTruNguoiPhuThuoc,
                NgayHieuLuc = newRecord.NgayHieuLuc,
                NgayHetHieuLuc = newRecord.NgayHetHieuLuc,
                GhiChu = newRecord.GhiChu
            };

            return CreatedAtAction(nameof(GetGiamTruGiaCanh), new { id = newRecord.Id }, responseDto);
        }

        [HttpPut("giam-tru-gia-canh/{id}")]
        public async Task<ActionResult<GiamTruGiaCanhDto>> UpdateGiamTruGiaCanh(int id, [FromBody] GiamTruGiaCanhDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new { message = "ID không khớp." });
            }

            var existingRecord = await _context.GiamTruGiaCanhs
                .FirstOrDefaultAsync(g => g.Id == id && g.NgayHetHieuLuc == null);

            if (existingRecord == null)
            {
                return BadRequest(new { message = "Chỉ có thể chỉnh sửa bản ghi mới nhất (chưa có ngày hết hiệu lực)." });
            }

            // Validate input
            if (dto.MucGiamTruBanThan < 0 || dto.MucGiamTruNguoiPhuThuoc < 0)
            {
                return BadRequest(new { message = "Mức giảm trừ không được âm." });
            }

            if (dto.NgayHieuLuc == default)
            {
                return BadRequest(new { message = "Ngày hiệu lực không hợp lệ." });
            }

            if (dto.NgayHetHieuLuc.HasValue && dto.NgayHetHieuLuc <= dto.NgayHieuLuc)
            {
                return BadRequest(new { message = "Ngày hết hiệu lực phải lớn hơn ngày hiệu lực." });
            }

            // Check for overlapping date ranges
            var overlappingRecord = await _context.GiamTruGiaCanhs
                .FirstOrDefaultAsync(g => g.Id != id &&
                    g.NgayHieuLuc <= dto.NgayHieuLuc &&
                    (g.NgayHetHieuLuc == null || g.NgayHetHieuLuc >= dto.NgayHieuLuc));

            if (overlappingRecord != null)
            {
                return BadRequest(new { message = "Ngày hiệu lực trùng với bản ghi hiện có." });
            }

            // Update record
            existingRecord.MucGiamTruBanThan = dto.MucGiamTruBanThan;
            existingRecord.MucGiamTruNguoiPhuThuoc = dto.MucGiamTruNguoiPhuThuoc;
            existingRecord.NgayHieuLuc = dto.NgayHieuLuc;
            existingRecord.NgayHetHieuLuc = dto.NgayHetHieuLuc;
            existingRecord.GhiChu = dto.GhiChu;

            _context.Entry(existingRecord).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.GiamTruGiaCanhs.AnyAsync(g => g.Id == id))
                {
                    return NotFound(new { message = $"Không tìm thấy bản ghi giảm trừ gia cảnh với ID {id}." });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi cập nhật bản ghi.", detail = ex.Message });
            }

            var responseDto = new GiamTruGiaCanhDto
            {
                Id = existingRecord.Id,
                MucGiamTruBanThan = existingRecord.MucGiamTruBanThan,
                MucGiamTruNguoiPhuThuoc = existingRecord.MucGiamTruNguoiPhuThuoc,
                NgayHieuLuc = existingRecord.NgayHieuLuc,
                NgayHetHieuLuc = existingRecord.NgayHetHieuLuc,
                GhiChu = existingRecord.GhiChu
            };

            return Ok(responseDto);
        }

        [HttpDelete("giam-tru-gia-canh/{id}")]
        public async Task<IActionResult> DeleteGiamTruGiaCanh(int id)
        {
            var record = await _context.GiamTruGiaCanhs.FindAsync(id);
            if (record == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi giảm trừ gia cảnh với ID {id}." });
            }

            _context.GiamTruGiaCanhs.Remove(record);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi xóa bản ghi.", detail = ex.Message });
            }

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
    // DTOs for GiamTruGiaCanh
    public class GiamTruGiaCanhDto
    {
        public int Id { get; set; }
        public decimal MucGiamTruBanThan { get; set; }
        public decimal MucGiamTruNguoiPhuThuoc { get; set; }
        public DateTime NgayHieuLuc { get; set; }
        public DateTime? NgayHetHieuLuc { get; set; }
        public string GhiChu { get; set; }
    }
}