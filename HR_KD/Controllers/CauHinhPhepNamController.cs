using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;
using HR_KD.ViewModels;

namespace HR_KD.Controllers
{
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

            // Lấy tất cả chính sách hiện có
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

            // Kiểm tra xem đã có cấu hình cho năm hiện tại chưa
            var existingConfig = await _context.CauHinhPhepNams
                .FirstOrDefaultAsync(c => c.Nam == DateTime.Now.Year);

            if (existingConfig != null)
            {
                TempData["Warning"] = $"Đã tồn tại cấu hình cho năm {DateTime.Now.Year}. Bạn không thể tạo thêm cấu hình cho năm hiện tại.";
            }

            // Lấy tất cả chính sách hiện có
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
                // Kiểm tra xem đã có cấu hình cho năm này chưa
                var existingConfig = await _context.CauHinhPhepNams
                    .FirstOrDefaultAsync(c => c.Nam == viewModel.Nam);

                if (existingConfig != null)
                {
                    ModelState.AddModelError("Nam", "Đã tồn tại cấu hình cho năm này");

                    // Lấy lại danh sách chính sách
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

                var cauHinhPhepNam = new CauHinhPhepNam
                {
                    Nam = viewModel.Nam,
                    SoNgayPhepMacDinh = viewModel.SoNgayPhepMacDinh,
                    CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>()
                };

                _context.CauHinhPhepNams.Add(cauHinhPhepNam);
                await _context.SaveChangesAsync();

                // Tạo các liên kết với chính sách đã chọn
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

            // Nếu có lỗi, lấy lại danh sách chính sách
            var allChinhSach = await _context.ChinhSachPhepNams
                .Where(c => c.ConHieuLuc)
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = allChinhSach.Select(c => new ChinhSachPhepNamListItem
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

            // Kiểm tra xem đây có phải là cấu hình của năm hiện tại không
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

            // Lấy tất cả chính sách hiện có
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
                return NotFound();
            }

            // Kiểm tra xem đây có phải là cấu hình của năm hiện tại không
            var currentCauHinh = await _context.CauHinhPhepNams.FindAsync(id);
            if (currentCauHinh != null && currentCauHinh.Nam == DateTime.Now.Year)
            {
                TempData["Error"] = "Không thể chỉnh sửa cấu hình của năm hiện tại vì đang được sử dụng.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra xem có cấu hình khác cho năm này không
                    var existingConfig = await _context.CauHinhPhepNams
                        .FirstOrDefaultAsync(c => c.Nam == viewModel.Nam && c.Id != id);

                    if (existingConfig != null)
                    {
                        ModelState.AddModelError("Nam", "Đã tồn tại cấu hình cho năm này");

                        // Lấy lại danh sách chính sách
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

                    var cauHinhPhepNam = await _context.CauHinhPhepNams
                        .Include(c => c.CauHinhPhep_ChinhSachs)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (cauHinhPhepNam == null)
                    {
                        return NotFound();
                    }

                    cauHinhPhepNam.Nam = viewModel.Nam;
                    cauHinhPhepNam.SoNgayPhepMacDinh = viewModel.SoNgayPhepMacDinh;

                    // Xóa các liên kết cũ
                    _context.CauHinhPhep_ChinhSachs.RemoveRange(cauHinhPhepNam.CauHinhPhep_ChinhSachs);

                    // Tạo các liên kết mới
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
                    }

                    _context.Update(cauHinhPhepNam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CauHinhPhepNamExists(viewModel.Id))
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

            // Nếu có lỗi, lấy lại danh sách chính sách
            var allChinhSach = await _context.ChinhSachPhepNams
                .OrderBy(c => c.SoNam)
                .ToListAsync();

            viewModel.DanhSachChinhSach = allChinhSach.Select(c => new ChinhSachPhepNamListItem
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

            // Kiểm tra xem đây có phải là cấu hình của năm hiện tại không
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

            // Kiểm tra xem đây có phải là cấu hình của năm hiện tại không
            if (cauHinhPhepNam.Nam == DateTime.Now.Year)
            {
                TempData["Error"] = "Không thể xóa cấu hình của năm hiện tại vì đang được sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa các liên kết trước
            _context.CauHinhPhep_ChinhSachs.RemoveRange(cauHinhPhepNam.CauHinhPhep_ChinhSachs);

            // Sau đó xóa cấu hình
            _context.CauHinhPhepNams.Remove(cauHinhPhepNam);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool CauHinhPhepNamExists(int id)
        {
            return _context.CauHinhPhepNams.Any(e => e.Id == id);
        }

        // Kiểm tra xem cấu hình có phải là của năm hiện tại không
        private bool IsCurrentYearConfig(int nam)
        {
            return nam == DateTime.Now.Year;
        }
    }
}