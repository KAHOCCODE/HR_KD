using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HR_KD.Controllers
{
    public class DaoTaoController : Controller
    {
        private readonly HrDbContext _context;

        public DaoTaoController(HrDbContext context)
        {
            _context = context;
        }

        // GET: DaoTao/Index (Dành cho quản lý)
        //[Authorize(Roles = "EMPLOYEE_MANAGER")] // Giả định có phân quyền
        public async Task<IActionResult> Index()
        {
            var daoTaos = await _context.DaoTaos
                .Include(d => d.PhongBan)
                .Select(d => new DaoTaoDTO
                {
                    MaDaoTao = d.MaDaoTao,
                    TenDaoTao = d.TenDaoTao,
                    MoTa = d.MoTa,
                    NoiDung = d.NoiDung,
                    NgayBatDau = d.NgayBatDau,
                    NgayKetThuc = d.NgayKetThuc,
                    MaPhongBan = d.MaPhongBan,
                    TenPhongBan = d.PhongBan.TenPhongBan
                })
                .ToListAsync();
            return View(daoTaos);
        }

        // GET: DaoTao/Details/5 (Dành cho quản lý)
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> Details(int id)
        {
            var daoTao = await _context.DaoTaos
                .Include(d => d.PhongBan)
                .Select(d => new DaoTaoDTO
                {
                    MaDaoTao = d.MaDaoTao,
                    TenDaoTao = d.TenDaoTao,
                    MoTa = d.MoTa,
                    NoiDung = d.NoiDung,
                    NgayBatDau = d.NgayBatDau,
                    NgayKetThuc = d.NgayKetThuc,
                    MaPhongBan = d.MaPhongBan,
                    TenPhongBan = d.PhongBan.TenPhongBan
                })
                .FirstOrDefaultAsync(d => d.MaDaoTao == id);

            if (daoTao == null) return NotFound();

            var lichSuDaoTaos = await _context.LichSuDaoTaos
                .Include(ls => ls.MaNvNavigation)
                .Where(ls => ls.MaDaoTao == id)
                .Select(ls => new LichSuDaoTaoDTO
                {
                    MaLichSu = ls.MaLichSu,
                    MaNv = ls.MaNv,
                    HoTen = ls.MaNvNavigation.HoTen,
                    MaDaoTao = ls.MaDaoTao,
                    KetQua = ls.KetQua
                })
                .ToListAsync();

            ViewBag.LichSuDaoTaos = lichSuDaoTaos;
            return View(daoTao);
        }

        // GET: DaoTao/Create (Dành cho quản lý)
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Create()
        {
            ViewBag.PhongBans = new SelectList(_context.PhongBans, "MaPhongBan", "TenPhongBan");
            return View();
        }

        // POST: DaoTao/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> Create(DaoTaoDTO dto)
        {
            if (ModelState.IsValid)
            {
                var daoTao = new DaoTao
                {
                    TenDaoTao = dto.TenDaoTao,
                    MoTa = dto.MoTa,
                    NoiDung = dto.NoiDung,
                    NgayBatDau = dto.NgayBatDau,
                    NgayKetThuc = dto.NgayKetThuc,
                    MaPhongBan = dto.MaPhongBan
                };

                _context.DaoTaos.Add(daoTao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PhongBans = new SelectList(_context.PhongBans, "MaPhongBan", "TenPhongBan", dto.MaPhongBan);
            return View(dto);
        }

        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var daoTao = await _context.DaoTaos
                .Include(d => d.LichSuDaoTaos)
                .ThenInclude(ls => ls.MaNvNavigation) // Include để lấy thông tin nhân viên
                .FirstOrDefaultAsync(d => d.MaDaoTao == id);

            if (daoTao == null) return NotFound();

            var dto = new DaoTaoDTO
            {
                MaDaoTao = daoTao.MaDaoTao,
                TenDaoTao = daoTao.TenDaoTao,
                MoTa = daoTao.MoTa,
                NoiDung = daoTao.NoiDung,
                NgayBatDau = daoTao.NgayBatDau,
                NgayKetThuc = daoTao.NgayKetThuc,
                MaPhongBan = daoTao.MaPhongBan
            };

            // Truyền danh sách nhân viên đã gán vào ViewBag
            ViewBag.AssignedEmployees = daoTao.LichSuDaoTaos.Select(ls => new
            {
                MaLichSu = ls.MaLichSu,
                MaNv = ls.MaNv,
                HoTen = ls.MaNvNavigation.HoTen
            }).ToList();

            ViewBag.PhongBans = new SelectList(_context.PhongBans, "MaPhongBan", "TenPhongBan", dto.MaPhongBan);
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, DaoTaoDTO dto)
        {
            if (id != dto.MaDaoTao) return BadRequest();

            if (ModelState.IsValid)
            {
                var daoTao = await _context.DaoTaos
                    .Include(d => d.LichSuDaoTaos) // Include để lấy danh sách nhân viên đã gán
                    .FirstOrDefaultAsync(d => d.MaDaoTao == id);

                if (daoTao == null) return NotFound();

                // Kiểm tra nếu MaPhongBan thay đổi
                if (daoTao.MaPhongBan != dto.MaPhongBan)
                {
                    // Xóa tất cả bản ghi LichSuDaoTao liên quan đến khóa đào tạo này
                    _context.LichSuDaoTaos.RemoveRange(daoTao.LichSuDaoTaos);
                }

                // Cập nhật thông tin khóa đào tạo
                daoTao.TenDaoTao = dto.TenDaoTao;
                daoTao.MoTa = dto.MoTa;
                daoTao.NoiDung = dto.NoiDung;
                daoTao.NgayBatDau = dto.NgayBatDau;
                daoTao.NgayKetThuc = dto.NgayKetThuc;
                daoTao.MaPhongBan = dto.MaPhongBan;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PhongBans = new SelectList(_context.PhongBans, "MaPhongBan", "TenPhongBan", dto.MaPhongBan);
            return View(dto);
        }

        // GET: DaoTao/Delete/5 (Dành cho quản lý)
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> Delete(int id)
        {
            var daoTao = await _context.DaoTaos
                .Include(d => d.PhongBan)
                .FirstOrDefaultAsync(d => d.MaDaoTao == id);
            if (daoTao == null) return NotFound();

            var dto = new DaoTaoDTO
            {
                MaDaoTao = daoTao.MaDaoTao,
                TenDaoTao = daoTao.TenDaoTao,
                MoTa = daoTao.MoTa,
                NoiDung = daoTao.NoiDung,
                NgayBatDau = daoTao.NgayBatDau,
                NgayKetThuc = daoTao.NgayKetThuc,
                TenPhongBan = daoTao.PhongBan.TenPhongBan // Đảm bảo gán TenPhongBan
            };
            return View(dto);
        }

        // POST: DaoTao/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var daoTao = await _context.DaoTaos.FindAsync(id);
            if (daoTao != null)
            {
                _context.DaoTaos.Remove(daoTao);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: DaoTao/Assign/5 (Dành cho quản lý)
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> Assign(int id)
        {
            var daoTao = await _context.DaoTaos.FindAsync(id);
            if (daoTao == null) return NotFound();

            var assignedMaNvs = _context.LichSuDaoTaos
                .Where(ls => ls.MaDaoTao == id)
                .Select(ls => ls.MaNv)
                .ToList();

            var employees = await _context.NhanViens
                .Where(nv => nv.MaPhongBan == daoTao.MaPhongBan && !assignedMaNvs.Contains(nv.MaNv))
                .ToListAsync();

            ViewBag.DaoTao = daoTao;
            return View(employees);
        }

        // POST: DaoTao/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> Assign(int maDaoTao, List<int> maNvs)
        {
            if (maNvs == null || maNvs.Count == 0)
                return RedirectToAction(nameof(Details), new { id = maDaoTao });

            var daoTao = await _context.DaoTaos.FindAsync(maDaoTao);
            if (daoTao == null) return NotFound();

            var employees = await _context.NhanViens
                .Where(nv => maNvs.Contains(nv.MaNv) && nv.MaPhongBan == daoTao.MaPhongBan)
                .ToListAsync();

            if (employees.Count != maNvs.Count)
                return RedirectToAction(nameof(Assign), new { id = maDaoTao });

            foreach (var maNv in maNvs)
            {
                if (!_context.LichSuDaoTaos.Any(ls => ls.MaNv == maNv && ls.MaDaoTao == maDaoTao))
                {
                    _context.LichSuDaoTaos.Add(new LichSuDaoTao
                    {
                        MaNv = maNv,
                        MaDaoTao = maDaoTao,
                        KetQua = "Chưa Hoàn Thành"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = maDaoTao });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
       // [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UnassignEmployee(int maLichSu, int maDaoTao)
        {
            var lichSu = await _context.LichSuDaoTaos.FindAsync(maLichSu);
            if (lichSu == null || lichSu.MaDaoTao != maDaoTao) return NotFound();

            _context.LichSuDaoTaos.Remove(lichSu);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = maDaoTao });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> UnassignAllEmployees(int maDaoTao)
        {
            var daoTao = await _context.DaoTaos
                .Include(d => d.LichSuDaoTaos)
                .FirstOrDefaultAsync(d => d.MaDaoTao == maDaoTao);

            if (daoTao == null) return NotFound();

            _context.LichSuDaoTaos.RemoveRange(daoTao.LichSuDaoTaos);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = maDaoTao });
        }

        // GET: DaoTao/ViewTraining (Dành cho nhân viên)
        [Authorize] // Giả định nhân viên đã đăng nhập
        public async Task<IActionResult> ViewTraining()
        {
            int maNv = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // Lấy MaNv từ Claims

            var lichSuDaoTaos = await _context.LichSuDaoTaos
                .Include(ls => ls.MaDaoTaoNavigation)
                .ThenInclude(d => d.PhongBan)
                .Where(ls => ls.MaNv == maNv)
                .Select(ls => new
                {
                    DaoTao = new DaoTaoDTO
                    {
                        MaDaoTao = ls.MaDaoTaoNavigation.MaDaoTao,
                        TenDaoTao = ls.MaDaoTaoNavigation.TenDaoTao,
                        MoTa = ls.MaDaoTaoNavigation.MoTa,
                        NoiDung = ls.MaDaoTaoNavigation.NoiDung,
                        NgayBatDau = ls.MaDaoTaoNavigation.NgayBatDau,
                        NgayKetThuc = ls.MaDaoTaoNavigation.NgayKetThuc,
                        MaPhongBan = ls.MaDaoTaoNavigation.MaPhongBan,
                        TenPhongBan = ls.MaDaoTaoNavigation.PhongBan.TenPhongBan
                    },
                    LichSu = new LichSuDaoTaoDTO
                    {
                        MaLichSu = ls.MaLichSu,
                        MaNv = ls.MaNv,
                        MaDaoTao = ls.MaDaoTao,
                        KetQua = ls.KetQua
                    }
                })
                .ToListAsync();

            return View(lichSuDaoTaos);
        }

        // POST: DaoTao/CompleteTraining (Dành cho nhân viên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CompleteTraining(int maLichSu)
        {
            var lichSu = await _context.LichSuDaoTaos.FindAsync(maLichSu);
            if (lichSu == null) return NotFound();

            int maNv = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (lichSu.MaNv != maNv) return Forbid();

            lichSu.KetQua = "Hoàn Thành";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewTraining));
        }
    }
}