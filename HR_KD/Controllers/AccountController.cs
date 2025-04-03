using HR_KD.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BCrypt.Net;
using HR_KD.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HR_KD.Controllers
{
    public class AccountController : Controller
    {
        private readonly HrDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.TaiKhoans
                .Include(t => t.TaiKhoanQuyenHans) // ✅ Load quyền hạn của tài khoản
                .FirstOrDefault(x => x.Username == model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                TempData["Error"] = "Sai tài khoản hoặc mật khẩu";
                return View(model);
            }

            var nhanVien = _context.NhanViens.FirstOrDefault(nv => nv.MaNv == user.MaNv);
            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin nhân viên";
                return View(model);
            }

            // ✅ Lấy danh sách quyền từ bảng TaiKhoanQuyenHan
            var userRoles = user.TaiKhoanQuyenHans.Select(q => q.MaQuyenHan).ToList();
            var validRoles = new List<string> { "EMPLOYEE", "EMPLOYEE_MANAGER", "LINE_MANAGER" };
            var assignedRole = userRoles.FirstOrDefault(role => validRoles.Contains(role)) ?? "EMPLOYEE"; // ✅ Kiểm tra quyền hợp lệ

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, nhanVien.HoTen),
                new Claim(ClaimTypes.Role, assignedRole), // ✅ Gán quyền hợp lệ
                new Claim("MaNV", user.MaNv.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // ✅ Lưu HoTen vào Session thay vì Username
            _httpContextAccessor.HttpContext.Session.SetString("HoTen", nhanVien.HoTen);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _httpContextAccessor.HttpContext.Session.Clear(); // ✅ Xóa toàn bộ Session khi logout
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
