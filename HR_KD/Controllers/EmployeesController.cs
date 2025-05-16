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
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(HrDbContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Danh sách nhân viên
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region Thêm nhân viên
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> Create(NhanVien nhanVien, ThongTinLuongNVDTO salaryDTO)
        {
            if (ModelState.IsValid)
            {
                _context.NhanViens.Add(nhanVien);
                await _context.SaveChangesAsync();

                // Gọi API để thiết lập lương
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("/api/"); // Thay bằng URL API của bạn
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
        #endregion

        #region Thiết lập lương
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
                .OrderByDescending(s => s.NgayApDung)
                .FirstOrDefaultAsync();

            if (existingSalary != null)
            {
                return RedirectToAction("Index"); // Hoặc hiển thị thông báo rằng nhân viên đã có lương
            }

            ViewBag.MaNv = maNv;
            return View();
        }
        #endregion

        #region Quản lý hợp đồng
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult EmployeeContracts()
        {
            return View();
        }

        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> SetupContracts(int maNv)
        {
            var nhanVien = await _context.NhanViens.FindAsync(maNv);
            if (nhanVien == null)
            {
                return NotFound();
            }

            ViewBag.MaNv = maNv;
            return View();
        }
        #endregion

        #region Chỉnh sửa lương
        public IActionResult EditSalary(int maLuongNV, int maNv)
        {
            ViewData["MaLuongNV"] = maLuongNV;
            ViewData["MaNv"] = maNv;

            var model = new ThongTinLuongNVDTO
            {
                MaLuongNV = maLuongNV,
                MaNv = maNv
            };

            return View(model);
        }
        #endregion

        #region Phân quyền
        public IActionResult Roles()
        {
            var currentUser = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Include(t => t.TaiKhoanQuyenHans)
                .ThenInclude(t => t.QuyenHan)
                .AsNoTracking() // Tối ưu hiệu suất
                .FirstOrDefault(t => t.Username == User.Identity.Name);

            if (currentUser == null) return Unauthorized();

            var currentNv = currentUser.MaNvNavigation;

            bool isDirector = User.IsInRole("DIRECTOR");

            if (isDirector)
            {
                var allEmployees = _context.TaiKhoans
                    .Include(t => t.MaNvNavigation).ThenInclude(nv => nv.MaChucVuNavigation)
                    .Include(t => t.MaNvNavigation).ThenInclude(nv => nv.MaPhongBanNavigation)
                    .Include(t => t.TaiKhoanQuyenHans).ThenInclude(r => r.QuyenHan)
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(t => t.MaNvNavigation.MaPhongBanNavigation.TenPhongBan)
                    .ToDictionary(g => g.Key, g => g.ToList());

                ViewBag.AllRoles = _context.QuyenHans.AsNoTracking().ToList();
                ViewBag.IsDirector = true;
                ViewBag.DepartmentName = null;
                ViewBag.UserRoles = currentUser.TaiKhoanQuyenHans.Select(r => r.MaQuyenHan).ToList();
                return View(allEmployees);
            }
            else
            {
                var capDuoi = _context.TaiKhoans
                    .Include(t => t.MaNvNavigation).ThenInclude(nv => nv.MaChucVuNavigation)
                    .Include(t => t.MaNvNavigation).ThenInclude(nv => nv.MaPhongBanNavigation)
                    .Include(t => t.TaiKhoanQuyenHans).ThenInclude(r => r.QuyenHan)
                    .Where(t =>
                        t.MaNvNavigation.MaPhongBan == currentNv.MaPhongBan &&
                        t.MaNvNavigation.MaChucVu > currentNv.MaChucVu)
                    .AsNoTracking()
                    .ToList();

                ViewBag.AllRoles = _context.QuyenHans.AsNoTracking().ToList();
                ViewBag.IsDirector = false;
                ViewBag.DepartmentName = currentNv.MaPhongBanNavigation.TenPhongBan;
                ViewBag.UserRoles = currentUser.TaiKhoanQuyenHans.Select(r => r.MaQuyenHan).ToList();
                return View(capDuoi);
            }
        }

        [HttpPost]
        public JsonResult UpdateRoles([FromBody] AssignRoleDto dto)
        {
            var user = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .Include(t => t.TaiKhoanQuyenHans)
                .FirstOrDefault(x => x.Username == dto.Username);

            var current = _context.TaiKhoans
                .Include(x => x.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
                .Include(x => x.TaiKhoanQuyenHans)
                .ThenInclude(x => x.QuyenHan)
                .FirstOrDefault(x => x.Username == User.Identity.Name);

            if (user == null || current == null)
                return Json(new { success = false, message = "Tài khoản không hợp lệ" });

            bool isDirector = User.IsInRole("DIRECTOR");
            if (!isDirector)
            {
                if (user.MaNvNavigation.MaPhongBan != current.MaNvNavigation.MaPhongBan ||
                    user.MaNvNavigation.MaChucVu <= current.MaNvNavigation.MaChucVu)
                {
                    return Json(new { success = false, message = "Không đủ quyền" });
                }

                var roleHierarchy = new List<string> { "DIRECTOR", "EMPLOYEE_MANAGER", "LINE_MANAGER", "EMPLOYEE", "PAYROLL_AUDITOR", "LIMITED_EMPLOYEE_MANAGER" };
                var currentUserRoles = current.TaiKhoanQuyenHans.Select(r => r.MaQuyenHan).ToList();
                var highestCurrentRole = currentUserRoles
                    .Select(r => roleHierarchy.IndexOf(r))
                    .Min();

                foreach (var role in dto.Roles)
                {
                    var roleIndex = roleHierarchy.IndexOf(role);
                    if (roleIndex < highestCurrentRole)
                    {
                        return Json(new { success = false, message = $"Không thể gán vai trò {role} cao hơn vai trò hiện tại" });
                    }
                }
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
            _logger.LogInformation("User {Username} updated roles for {TargetUsername} to {Roles}", User.Identity.Name, dto.Username, string.Join(",", dto.Roles));
            return Json(new { success = true });
        }
        #endregion
    }

    public class AssignRoleDto
    {
        public string Username { get; set; }
        public List<string> Roles { get; set; }
    }
}

