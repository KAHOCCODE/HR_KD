using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;

using Microsoft.AspNetCore.Authorization;
using HR_KD.Models;

namespace HR_KD.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChinhSachPhepNamController : ControllerBase
    {
        private readonly HrDbContext _context;

        public ChinhSachPhepNamController(HrDbContext context)
        {
            _context = context;
        }

        // GET: api/ChinhSachPhepNam
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChinhSachPhepNamViewModel>>> GetChinhSachPhepNams(
             [FromQuery] string searchTerm = "",
             [FromQuery] int page = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] bool? conHieuLuc = null)
        {
            var query = _context.ChinhSachPhepNams.AsQueryable();

            // Áp dụng các bộ lọc
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Chuẩn hóa chuỗi tìm kiếm
                searchTerm = searchTerm.Trim().ToLower();

                // Tách từ khóa tìm kiếm thành các từ riêng biệt
                var keywords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (keywords.Length > 0)
                {
                    // Sử dụng điều kiện OR giữa các từ khóa
                    query = query.Where(c => keywords.Any(k =>
                        EF.Functions.Like(c.TenChinhSach.ToLower(), "%" + k + "%")));
                }
            }

            if (conHieuLuc.HasValue)
            {
                query = query.Where(c => c.ConHieuLuc == conHieuLuc.Value);
            }

            // Lấy tổng số mục
            var totalCount = await query.CountAsync();

            // Phân trang
            var items = await query
                .OrderBy(c => c.SoNam)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ChinhSachPhepNamViewModel
                {
                    Id = c.Id,
                    TenChinhSach = c.TenChinhSach,
                    SoNam = c.SoNam,
                    SoNgayCongThem = c.SoNgayCongThem,
                    ApDungTuNam = c.ApDungTuNam,
                    ConHieuLuc = c.ConHieuLuc
                })
                .ToListAsync();

            // Thêm header cho phân trang
            Response.Headers.Add("X-Total-Count", totalCount.ToString());
            Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalCount / pageSize).ToString());

            return items;
        }

        // GET: api/ChinhSachPhepNam/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChinhSachPhepNamViewModel>> GetChinhSachPhepNam(int id)
        {
            var chinhSachPhepNam = await _context.ChinhSachPhepNams.FindAsync(id);

            if (chinhSachPhepNam == null)
            {
                return NotFound();
            }

            var viewModel = new ChinhSachPhepNamViewModel
            {
                Id = chinhSachPhepNam.Id,
                TenChinhSach = chinhSachPhepNam.TenChinhSach,
                SoNam = chinhSachPhepNam.SoNam,
                SoNgayCongThem = chinhSachPhepNam.SoNgayCongThem,
                ApDungTuNam = chinhSachPhepNam.ApDungTuNam,
                ConHieuLuc = chinhSachPhepNam.ConHieuLuc
            };

            return viewModel;
        }

        // PUT: api/ChinhSachPhepNam/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChinhSachPhepNam(int id, ChinhSachPhepNamViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            var chinhSachPhepNam = await _context.ChinhSachPhepNams.FindAsync(id);
            if (chinhSachPhepNam == null)
            {
                return NotFound();
            }

            chinhSachPhepNam.TenChinhSach = viewModel.TenChinhSach;
            chinhSachPhepNam.SoNam = viewModel.SoNam;
            chinhSachPhepNam.SoNgayCongThem = viewModel.SoNgayCongThem;
            chinhSachPhepNam.ApDungTuNam = viewModel.ApDungTuNam;
            chinhSachPhepNam.ConHieuLuc = viewModel.ConHieuLuc;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChinhSachPhepNamExists(id))
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

        // POST: api/ChinhSachPhepNam
        [HttpPost]
        public async Task<ActionResult<ChinhSachPhepNamViewModel>> PostChinhSachPhepNam(ChinhSachPhepNamViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var chinhSachPhepNam = new ChinhSachPhepNam
            {
                TenChinhSach = viewModel.TenChinhSach,
                SoNam = viewModel.SoNam,
                SoNgayCongThem = viewModel.SoNgayCongThem,
                ApDungTuNam = viewModel.ApDungTuNam,
                ConHieuLuc = viewModel.ConHieuLuc
            };

            _context.ChinhSachPhepNams.Add(chinhSachPhepNam);
            await _context.SaveChangesAsync();

            viewModel.Id = chinhSachPhepNam.Id;

            return CreatedAtAction("GetChinhSachPhepNam", new { id = chinhSachPhepNam.Id }, viewModel);
        }

        // DELETE: api/ChinhSachPhepNam/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChinhSachPhepNam(int id)
        {
            var chinhSachPhepNam = await _context.ChinhSachPhepNams.FindAsync(id);
            if (chinhSachPhepNam == null)
            {
                return NotFound();
            }

            _context.ChinhSachPhepNams.Remove(chinhSachPhepNam);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //// PATCH: api/ChinhSachPhepNam/5/ToggleStatus
        //[HttpPatch("{id}/ToggleStatus")]
        //public async Task<IActionResult> ToggleStatus(int id)
        //{
        //    var chinhSachPhepNam = await _context.ChinhSachPhepNams.FindAsync(id);

        //    if (chinhSachPhepNam == null)
        //    {
        //        return NotFound();
        //    }

        //    chinhSachPhepNam.ConHieuLuc = !chinhSachPhepNam.ConHieuLuc;
        //    await _context.SaveChangesAsync();

        //    return NoContent();
        //}

        private bool ChinhSachPhepNamExists(int id)
        {
            return _context.ChinhSachPhepNams.Any(e => e.Id == id);
        }
    }
}