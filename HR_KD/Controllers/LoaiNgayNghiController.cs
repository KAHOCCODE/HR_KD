using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;

namespace HR_KD.Controllers
{
    public class LoaiNgayNghiController : Controller
    {
        private readonly HrDbContext _context;

        public LoaiNgayNghiController(HrDbContext context)
        {
            _context = context;
        }

        // GET: LoaiNgayNghi
        public async Task<IActionResult> Index()
        {
            return View(await _context.LoaiNgayNghis.ToListAsync());
        }

        //// GET: LoaiNgayNghi/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var loaiNgayNghi = await _context.LoaiNgayNghis
        //        .FirstOrDefaultAsync(m => m.MaLoaiNgayNghi == id);
        //    if (loaiNgayNghi == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(loaiNgayNghi);
        //}

        // GET: LoaiNgayNghi/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: LoaiNgayNghi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLoaiNgayNghi,TenLoai,MoTa,HuongLuong,TinhVaoPhepNam,CoTinhVaoLuong")] LoaiNgayNghi loaiNgayNghi)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loaiNgayNghi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Loại ngày nghỉ đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(loaiNgayNghi);
        }

        // GET: LoaiNgayNghi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiNgayNghi = await _context.LoaiNgayNghis.FindAsync(id);
            if (loaiNgayNghi == null)
            {
                return NotFound();
            }
            return View(loaiNgayNghi);
        }

        // POST: LoaiNgayNghi/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLoaiNgayNghi,TenLoai,MoTa,HuongLuong,TinhVaoPhepNam,CoTinhVaoLuong")] LoaiNgayNghi loaiNgayNghi)
        {
            if (id != loaiNgayNghi.MaLoaiNgayNghi)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loaiNgayNghi);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Loại ngày nghỉ đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaiNgayNghiExists(loaiNgayNghi.MaLoaiNgayNghi))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(loaiNgayNghi);
        }

        // GET: LoaiNgayNghi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaiNgayNghi = await _context.LoaiNgayNghis
                .FirstOrDefaultAsync(m => m.MaLoaiNgayNghi == id);
            if (loaiNgayNghi == null)
            {
                return NotFound();
            }

            return View(loaiNgayNghi);
        }

        // POST: LoaiNgayNghi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loaiNgayNghi = await _context.LoaiNgayNghis.FindAsync(id);
            if (loaiNgayNghi != null)
            {
                // Kiểm tra xem có ngày nghỉ nào đang sử dụng loại này không
                bool hasRelatedData = await _context.NgayNghis.AnyAsync(nn => nn.MaLoaiNgayNghi == id);
                if (hasRelatedData)
                {
                    TempData["ErrorMessage"] = "Không thể xóa loại ngày nghỉ này vì đang được sử dụng!";
                    return RedirectToAction(nameof(Index));
                }

                _context.LoaiNgayNghis.Remove(loaiNgayNghi);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Loại ngày nghỉ đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LoaiNgayNghiExists(int id)
        {
            return _context.LoaiNgayNghis.Any(e => e.MaLoaiNgayNghi == id);
        }
    }
}