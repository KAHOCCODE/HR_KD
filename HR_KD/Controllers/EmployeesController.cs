using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HrDbContext _context;

        public EmployeesController(HrDbContext context)
        {
            _context = context;
        }

        //[Authorize(Roles = "EMPLOYEE")]
        public IActionResult Index()
        {
            return View();
        }
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        // Trong NhanVienController.cs
        public async Task<IActionResult> Create(NhanVien nhanVien, ThongTinLuongNVDTO salaryDTO)
        {
            if (ModelState.IsValid)
            {
                _context.NhanViens.Add(nhanVien);
                await _context.SaveChangesAsync();

                // Gọi API để thiết lập lương
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://localhost:5012/api/"); // Thay bằng URL API của bạn
                    salaryDTO.MaNv = nhanVien.MaNv;
                    var response = await client.PostAsJsonAsync("SalaryApi/setup", salaryDTO);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Xử lý lỗi nếu không thiết lập được lương
                        ModelState.AddModelError("", "Không thể thiết lập lương cho nhân viên.");
                        return View(nhanVien);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(nhanVien);
        }
        public async Task<IActionResult> SetupSalary(int maNv)
        {
            var nhanVien = await _context.NhanViens.FindAsync(maNv);
            if (nhanVien == null)
            {
                return NotFound();
            }

            // Kiểm tra xem nhân viên đã có lương chưa
            var existingSalary = await _context.ThongTinLuongNVs
                .Where(s => s.MaNv == maNv)
                .OrderByDescending(s => s.NgayApDng)
                .FirstOrDefaultAsync();

            if (existingSalary != null)
            {
                return RedirectToAction("Index"); // Hoặc hiển thị thông báo rằng nhân viên đã có lương
            }

            ViewBag.MaNv = maNv;
            return View();
        }
        #region phân quyền 
        [Authorize(Policy = "CanManageSubordinates")]
        public IActionResult Roles()
        {
            var currentUser = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .FirstOrDefault(t => t.Username == User.Identity.Name);

            if (currentUser == null) return Unauthorized();

            var currentNv = currentUser.MaNvNavigation;

            // ✅ Tìm cấp dưới: cùng phòng ban, chức vụ thấp hơn
            var capDuoi = _context.TaiKhoans
                .Include(t => t.MaNvNavigation).ThenInclude(nv => nv.MaChucVuNavigation)
                .Include(t => t.TaiKhoanQuyenHans).ThenInclude(r => r.QuyenHan)
                .Where(t =>
                    t.MaNvNavigation.MaPhongBan == currentNv.MaPhongBan &&
                    t.MaNvNavigation.MaChucVu > currentNv.MaChucVu
                ).ToList();

            ViewBag.AllRoles = _context.QuyenHans.ToList();

            return View(capDuoi);
        }

        [HttpPost]
        public JsonResult UpdateRoles([FromBody] AssignRoleDto dto)
        {
            var user = _context.TaiKhoans.Include(t => t.MaNvNavigation)
                .FirstOrDefault(x => x.Username == dto.Username);

            var current = _context.TaiKhoans.Include(x => x.MaNvNavigation)
                .FirstOrDefault(x => x.Username == User.Identity.Name);

            if (user == null || current == null)
                return Json(new { success = false, message = "Tài khoản không hợp lệ" });

            if (user.MaNvNavigation.MaPhongBan != current.MaNvNavigation.MaPhongBan ||
                user.MaNvNavigation.MaChucVu <= current.MaNvNavigation.MaChucVu)
            {
                return Json(new { success = false, message = "Không đủ quyền" });
            }

            _context.TaiKhoanQuyenHans.RemoveRange(_context.TaiKhoanQuyenHans.Where(x => x.Username == dto.Username));
            foreach (var role in dto.Roles)
            {
                _context.TaiKhoanQuyenHans.Add(new TaiKhoanQuyenHan
                {
                    Username = dto.Username,
                    MaQuyenHan = role
                });
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        #endregion
    }
}
