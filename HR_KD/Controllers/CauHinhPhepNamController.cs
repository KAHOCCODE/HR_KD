using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using HR_KD.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace HR_KD.Controllers
{
    [Authorize(Roles = "DIRECTOR")]
    public class CauHinhPhepNamController : Controller
    {
        private readonly HrDbContext _context;

        public CauHinhPhepNamController(HrDbContext context)
        {
            _context = context;
        }

        // GET: CauHinhPhepNam
        public async Task<IActionResult> Index()
        {
            var cauHinhList = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .ThenInclude(c => c.ChinhSachPhepNam)
                .OrderByDescending(c => c.Nam)
                .ToListAsync();

            var viewModel = new CauHinhPhepNamListViewModel
            {
                DanhSachCauHinh = cauHinhList.Select(c => new CauHinhPhepNamItemViewModel
                {
                    Id = c.Id,
                    Nam = c.Nam,
                    SoNgayPhepMacDinh = c.SoNgayPhepMacDinh,
                    DanhSachTenChinhSach = c.CauHinhPhep_ChinhSachs
                        .Select(cs => cs.ChinhSachPhepNam.TenChinhSach)
                        .ToList()
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: CauHinhPhepNam/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cauHinhPhepNam = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .ThenInclude(c => c.ChinhSachPhepNam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cauHinhPhepNam == null)
            {
                return NotFound();
            }

            var viewModel = new CauHinhPhepNamViewModel
            {
                Id = cauHinhPhepNam.Id,
                Nam = cauHinhPhepNam.Nam,
                SoNgayPhepMacDinh = cauHinhPhepNam.SoNgayPhepMacDinh,
                ChinhSachPhepNamIds = cauHinhPhepNam.CauHinhPhep_ChinhSachs
                    .Select(c => c.ChinhSachPhepNamId)
                    .ToList(),
                IsCurrentYear = cauHinhPhepNam.Nam == DateTime.Now.Year
            };

            var danhSachChinhSach = await _context.ChinhSachPhepNams
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = danhSachChinhSach.Select(c => new ChinhSachPhepNamListItem
            {
                Id = c.Id,
                TenChinhSach = c.TenChinhSach,
                SoNam = c.SoNam,
                SoNgayCongThem = c.SoNgayCongThem,
                ApDungTuNam = c.ApDungTuNam,
                ConHieuLuc = c.ConHieuLuc,
                IsSelected = viewModel.ChinhSachPhepNamIds.Contains(c.Id)
            }).ToList();

            return View(viewModel);
        }

        // GET: CauHinhPhepNam/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CauHinhPhepNamViewModel
            {
                Nam = DateTime.Now.Year
            };

            var existingConfig = await _context.CauHinhPhepNams
                .FirstOrDefaultAsync(c => c.Nam == DateTime.Now.Year);

            if (existingConfig != null)
            {
                TempData["Warning"] = $"Đã tồn tại cấu hình cho năm {DateTime.Now.Year}. Bạn không thể tạo thêm cấu hình cho năm hiện tại.";
            }

            var danhSachChinhSach = await _context.ChinhSachPhepNams
                .Where(c => c.ConHieuLuc)
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = danhSachChinhSach.Select(c => new ChinhSachPhepNamListItem
            {
                Id = c.Id,
                TenChinhSach = c.TenChinhSach,
                SoNam = c.SoNam,
                SoNgayCongThem = c.SoNgayCongThem,
                ApDungTuNam = c.ApDungTuNam,
                ConHieuLuc = c.ConHieuLuc,
                IsSelected = false
            }).ToList();

            return View(viewModel);
        }

        // POST: CauHinhPhepNam/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CauHinhPhepNamViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var existingConfig = await _context.CauHinhPhepNams
                    .FirstOrDefaultAsync(c => c.Nam == viewModel.Nam);

                if (existingConfig != null)
                {
                    ModelState.AddModelError("Nam", "Đã tồn tại cấu hình cho năm này");
                }
                else
                {
                    var cauHinhPhepNam = new CauHinhPhepNam
                    {
                        Nam = viewModel.Nam,
                        SoNgayPhepMacDinh = viewModel.SoNgayPhepMacDinh,
                        CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>()
                    };

                    _context.CauHinhPhepNams.Add(cauHinhPhepNam);
                    await _context.SaveChangesAsync();

                    if (viewModel.ChinhSachPhepNamIds != null && viewModel.ChinhSachPhepNamIds.Count > 0)
                    {
                        foreach (var chinhSachId in viewModel.ChinhSachPhepNamIds)
                        {
                            var cauHinhPhep_ChinhSach = new CauHinhPhep_ChinhSach
                            {
                                CauHinhPhepNamId = cauHinhPhepNam.Id,
                                ChinhSachPhepNamId = chinhSachId
                            };
                            _context.CauHinhPhep_ChinhSachs.Add(cauHinhPhep_ChinhSach);
                        }
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }
            }

            var danhSachChinhSach = await _context.ChinhSachPhepNams
                .Where(c => c.ConHieuLuc)
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = danhSachChinhSach.Select(c => new ChinhSachPhepNamListItem
            {
                Id = c.Id,
                TenChinhSach = c.TenChinhSach,
                SoNam = c.SoNam,
                SoNgayCongThem = c.SoNgayCongThem,
                ApDungTuNam = c.ApDungTuNam,
                ConHieuLuc = c.ConHieuLuc,
                IsSelected = viewModel.ChinhSachPhepNamIds?.Contains(c.Id) ?? false
            }).ToList();

            return View(viewModel);
        }

        // GET: CauHinhPhepNam/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cauHinhPhepNam = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cauHinhPhepNam == null)
            {
                return NotFound();
            }

            if (cauHinhPhepNam.Nam == DateTime.Now.Year)
            {
                TempData["Error"] = "Không thể chỉnh sửa cấu hình của năm hiện tại vì đang được sử dụng.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var viewModel = new CauHinhPhepNamViewModel
            {
                Id = cauHinhPhepNam.Id,
                Nam = cauHinhPhepNam.Nam,
                SoNgayPhepMacDinh = cauHinhPhepNam.SoNgayPhepMacDinh,
                ChinhSachPhepNamIds = cauHinhPhepNam.CauHinhPhep_ChinhSachs
                    .Select(c => c.ChinhSachPhepNamId)
                    .ToList()
            };

            var danhSachChinhSach = await _context.ChinhSachPhepNams
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = danhSachChinhSach.Select(c => new ChinhSachPhepNamListItem
            {
                Id = c.Id,
                TenChinhSach = c.TenChinhSach,
                SoNam = c.SoNam,
                SoNgayCongThem = c.SoNgayCongThem,
                ApDungTuNam = c.ApDungTuNam,
                ConHieuLuc = c.ConHieuLuc,
                IsSelected = viewModel.ChinhSachPhepNamIds.Contains(c.Id)
            }).ToList();

            return View(viewModel);
        }

        // POST: CauHinhPhepNam/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CauHinhPhepNamViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest("ID không khớp với dữ liệu gửi lên.");
            }

            var currentCauHinh = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (currentCauHinh == null)
            {
                return NotFound();
            }

            if (currentCauHinh.Nam == DateTime.Now.Year)
            {
                TempData["Error"] = "Không thể chỉnh sửa cấu hình của năm hiện tại vì đang được sử dụng.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (viewModel.Nam != currentCauHinh.Nam)
                    {
                        var existingConfig = await _context.CauHinhPhepNams
                            .FirstOrDefaultAsync(c => c.Nam == viewModel.Nam && c.Id != id);

                        if (existingConfig != null)
                        {
                            ModelState.AddModelError("Nam", "Đã tồn tại cấu hình cho năm này");
                            return await ReloadEditView(viewModel);
                        }
                    }

                    currentCauHinh.Nam = viewModel.Nam;
                    currentCauHinh.SoNgayPhepMacDinh = viewModel.SoNgayPhepMacDinh;

                    _context.CauHinhPhep_ChinhSachs.RemoveRange(currentCauHinh.CauHinhPhep_ChinhSachs);

                    if (viewModel.ChinhSachPhepNamIds != null && viewModel.ChinhSachPhepNamIds.Count > 0)
                    {
                        foreach (var chinhSachId in viewModel.ChinhSachPhepNamIds)
                        {
                            var lienKet = new CauHinhPhep_ChinhSach
                            {
                                CauHinhPhepNamId = currentCauHinh.Id,
                                ChinhSachPhepNamId = chinhSachId
                            };
                            _context.CauHinhPhep_ChinhSachs.Add(lienKet);
                        }
                    }

                    _context.Update(currentCauHinh);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CauHinhPhepNamExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return await ReloadEditView(viewModel);
        }

        // GET: CauHinhPhepNam/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cauHinhPhepNam = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .ThenInclude(c => c.ChinhSachPhepNam)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cauHinhPhepNam == null)
            {
                return NotFound();
            }

            if (cauHinhPhepNam.Nam == DateTime.Now.Year)
            {
                TempData["Error"] = "Không thể xóa cấu hình của năm hiện tại vì đang được sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CauHinhPhepNamItemViewModel
            {
                Id = cauHinhPhepNam.Id,
                Nam = cauHinhPhepNam.Nam,
                SoNgayPhepMacDinh = cauHinhPhepNam.SoNgayPhepMacDinh,
                DanhSachTenChinhSach = cauHinhPhepNam.CauHinhPhep_ChinhSachs
                    .Select(cs => cs.ChinhSachPhepNam.TenChinhSach)
                    .ToList()
            };

            return View(viewModel);
        }

        // POST: CauHinhPhepNam/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cauHinhPhepNam = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (cauHinhPhepNam == null)
            {
                return NotFound();
            }

            if (cauHinhPhepNam.Nam == DateTime.Now.Year)
            {
                TempData["Error"] = "Không thể xóa cấu hình của năm hiện tại vì đang được sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _context.CauHinhPhep_ChinhSachs.RemoveRange(cauHinhPhepNam.CauHinhPhep_ChinhSachs);
            _context.CauHinhPhepNams.Remove(cauHinhPhepNam);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool CauHinhPhepNamExists(int id)
        {
            return _context.CauHinhPhepNams.Any(e => e.Id == id);
        }

        private async Task<IActionResult> ReloadEditView(CauHinhPhepNamViewModel viewModel)
        {
            var danhSachChinhSach = await _context.ChinhSachPhepNams
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = danhSachChinhSach.Select(c => new ChinhSachPhepNamListItem
            {
                Id = c.Id,
                TenChinhSach = c.TenChinhSach,
                SoNam = c.SoNam,
                SoNgayCongThem = c.SoNgayCongThem,
                ApDungTuNam = c.ApDungTuNam,
                ConHieuLuc = c.ConHieuLuc,
                IsSelected = viewModel.ChinhSachPhepNamIds?.Contains(c.Id) ?? false
            }).ToList();

            return View(viewModel);
        }
    }
}