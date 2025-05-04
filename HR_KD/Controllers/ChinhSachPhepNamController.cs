using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using HR_KD.Models;

namespace HR_KD.Controllers
{
    [Authorize]
    public class ChinhSachPhepNamController : Controller
    {
        private readonly HrDbContext _context;

        public ChinhSachPhepNamController(HrDbContext context)
        {
            _context = context;
        }

        // GET: ChinhSachPhepNam
        public async Task<IActionResult> Index(int pageIndex = 1, int pageSize = 10, string searchString = "")
        {
            var query = _context.ChinhSachPhepNams.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Chuẩn hóa chuỗi tìm kiếm
                searchString = searchString.Trim().ToLower();

                // Tách từ khóa tìm kiếm thành các từ riêng biệt
                var keywords = searchString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Tìm kiếm bản ghi chứa bất kỳ từ khóa nào
                foreach (var keyword in keywords)
                {
                    query = query.Where(c => EF.Functions.Like(c.TenChinhSach.ToLower(), "%" + keyword + "%"));
                }
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageSize)
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

            var viewModel = new ChinhSachPhepNamListViewModel
            {
                ChinhSachPhepNams = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            ViewBag.SearchString = searchString;
            return View(viewModel);
        }

        private IQueryable<ChinhSachPhepNam> ApplySearch(IQueryable<ChinhSachPhepNam> query, string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
                return query;

            // Chuẩn hóa chuỗi tìm kiếm
            searchString = searchString.Trim().ToLower();

            // Tách từ khóa tìm kiếm thành các từ riêng biệt
            var keywords = searchString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (keywords.Length == 0)
                return query;

            // Tìm các bản ghi có chứa ít nhất một từ khóa (điều kiện OR)
            return query.Where(c => keywords.Any(k =>
                EF.Functions.Like(c.TenChinhSach.ToLower(), "%" + k + "%")));
        }
        // GET: ChinhSachPhepNam/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chinhSachPhepNam = await _context.ChinhSachPhepNams
                .FirstOrDefaultAsync(m => m.Id == id);

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

            return View(viewModel);
        }

        // GET: ChinhSachPhepNam/Create
        public IActionResult Create()
        {
            var viewModel = new ChinhSachPhepNamViewModel
            {
                ConHieuLuc = true,
                ApDungTuNam = DateTime.Now.Year
            };

            return View(viewModel);
        }

        // POST: ChinhSachPhepNam/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChinhSachPhepNamViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var chinhSachPhepNam = new ChinhSachPhepNam
                {
                    TenChinhSach = viewModel.TenChinhSach,
                    SoNam = viewModel.SoNam,
                    SoNgayCongThem = viewModel.SoNgayCongThem,
                    ApDungTuNam = viewModel.ApDungTuNam,
                    ConHieuLuc = viewModel.ConHieuLuc
                };

                _context.Add(chinhSachPhepNam);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm chính sách phép năm thành công";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: ChinhSachPhepNam/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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

            return View(viewModel);
        }

        // POST: ChinhSachPhepNam/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChinhSachPhepNamViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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

                    _context.Update(chinhSachPhepNam);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật chính sách phép năm thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChinhSachPhepNamExists(viewModel.Id))
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
            return View(viewModel);
        }

        // GET: ChinhSachPhepNam/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chinhSachPhepNam = await _context.ChinhSachPhepNams
                .FirstOrDefaultAsync(m => m.Id == id);
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

            return View(viewModel);
        }

        // POST: ChinhSachPhepNam/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chinhSachPhepNam = await _context.ChinhSachPhepNams.FindAsync(id);

            if (chinhSachPhepNam != null)
            {
                _context.ChinhSachPhepNams.Remove(chinhSachPhepNam);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa chính sách phép năm thành công";
            }

            return RedirectToAction(nameof(Index));
        }

        //// Chuyển trạng thái hiệu lực
        //// Chuyển trạng thái hiệu lực
        //[HttpPost]
        //[ValidateAntiForgeryToken]  // Thêm thuộc tính này
        //public async Task<IActionResult> ToggleStatus(int id)
        //{
        //    var chinhSachPhepNam = await _context.ChinhSachPhepNams.FindAsync(id);

        //    if (chinhSachPhepNam == null)
        //    {
        //        return NotFound();
        //    }

        //    chinhSachPhepNam.ConHieuLuc = !chinhSachPhepNam.ConHieuLuc;
        //    await _context.SaveChangesAsync();

        //    // Thêm thông báo thành công
        //    TempData["SuccessMessage"] = chinhSachPhepNam.ConHieuLuc
        //        ? "Kích hoạt chính sách phép năm thành công"
        //        : "Vô hiệu hóa chính sách phép năm thành công";

        //    return RedirectToAction(nameof(Index));
        //}
        private bool ChinhSachPhepNamExists(int id)
        {
            return _context.ChinhSachPhepNams.Any(e => e.Id == id);
        }
    }
}