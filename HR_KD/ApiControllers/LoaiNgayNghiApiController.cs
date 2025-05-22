using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;

namespace HR_KD.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "DIRECTOR")]
    public class LoaiNgayNghiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public LoaiNgayNghiController(HrDbContext context)
        {
            _context = context;
        }

        // GET: api/LoaiNgayNghi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LoaiNgayNghi>>> GetLoaiNgayNghi()
        {
            return await _context.LoaiNgayNghis.ToListAsync();
        }

        // GET: api/LoaiNgayNghi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LoaiNgayNghi>> GetLoaiNgayNghi(int id)
        {
            var loaiNgayNghi = await _context.LoaiNgayNghis.FindAsync(id);

            if (loaiNgayNghi == null)
            {
                return NotFound();
            }

            return loaiNgayNghi;
        }

        // PUT: api/LoaiNgayNghi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLoaiNgayNghi(int id, LoaiNgayNghi loaiNgayNghi)
        {
            if (id != loaiNgayNghi.MaLoaiNgayNghi)
            {
                return BadRequest();
            }

            // Kiểm tra tính hợp lệ
            if (string.IsNullOrWhiteSpace(loaiNgayNghi.TenLoai))
            {
                return BadRequest(new { message = "Tên loại ngày nghỉ không được để trống" });
            }

            // Kiểm tra tính hợp lệ cho các trường mới
            if (loaiNgayNghi.SoNgayNghiToiDa.HasValue && loaiNgayNghi.SoNgayNghiToiDa < 0)
            {
                return BadRequest(new { message = "Số ngày nghỉ tối đa không được nhỏ hơn 0" });
            }

            if (loaiNgayNghi.SoLanDangKyToiDa.HasValue && loaiNgayNghi.SoLanDangKyToiDa < 0)
            {
                return BadRequest(new { message = "Số lần đăng ký tối đa không được nhỏ hơn 0" });
            }

            _context.Entry(loaiNgayNghi).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LoaiNgayNghiExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/LoaiNgayNghi
        [HttpPost]
        public async Task<ActionResult<LoaiNgayNghi>> PostLoaiNgayNghi(LoaiNgayNghi loaiNgayNghi)
        {
            // Kiểm tra tính hợp lệ
            if (string.IsNullOrWhiteSpace(loaiNgayNghi.TenLoai))
            {
                return BadRequest(new { message = "Tên loại ngày nghỉ không được để trống" });
            }

            // Kiểm tra tính hợp lệ cho các trường mới
            if (loaiNgayNghi.SoNgayNghiToiDa.HasValue && loaiNgayNghi.SoNgayNghiToiDa < 0)
            {
                return BadRequest(new { message = "Số ngày nghỉ tối đa không được nhỏ hơn 0" });
            }

            if (loaiNgayNghi.SoLanDangKyToiDa.HasValue && loaiNgayNghi.SoLanDangKyToiDa < 0)
            {
                return BadRequest(new { message = "Số lần đăng ký tối đa không được nhỏ hơn 0" });
            }

            _context.LoaiNgayNghis.Add(loaiNgayNghi);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetLoaiNgayNghi", new { id = loaiNgayNghi.MaLoaiNgayNghi }, loaiNgayNghi);
        }

        // DELETE: api/LoaiNgayNghi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLoaiNgayNghi(int id)
        {
            var loaiNgayNghi = await _context.LoaiNgayNghis.FindAsync(id);
            if (loaiNgayNghi == null)
            {
                return NotFound();
            }

            // Kiểm tra xem có ngày nghỉ nào đang sử dụng loại này không
            bool hasRelatedData = await _context.NgayNghis.AnyAsync(nn => nn.MaLoaiNgayNghi == id);
            if (hasRelatedData)
            {
                return BadRequest(new { message = "Không thể xóa loại ngày nghỉ này vì đang được sử dụng!" });
            }

            _context.LoaiNgayNghis.Remove(loaiNgayNghi);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool LoaiNgayNghiExists(int id)
        {
            return _context.LoaiNgayNghis.Any(e => e.MaLoaiNgayNghi == id);
        }
    }
}



