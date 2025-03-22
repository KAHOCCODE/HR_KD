using HR_KD.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BCrypt.Net;
using HR_KD.Models;
using Microsoft.AspNetCore.Http; // 🔹 Thêm thư viện này

namespace HR_KD.Controllers
{
    public class AccountController : Controller
    {
        private readonly HrDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor; // 🔹 Inject IHttpContextAccessor

        public AccountController(HrDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor; // 🔹 Lưu lại HttpContextAccessor
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

            var user = _context.TaiKhoans.FirstOrDefault(x => x.Username == model.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user?.PasswordHash ?? ""))
            {
                TempData["Error"] = "Sai tài khoản hoặc mật khẩu";
                return View(model);
            }

            // Tạo danh sách quyền hợp lệ
            var validRoles = new List<string> { "EMPLOYEE", "EMPLOYEE_MANAGER", "LINE_MANAGER" };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, validRoles.Contains(user.MaQuyenHan) ? user.MaQuyenHan : "EMPLOYEE")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // 🔹 Lưu Username vào Session
            _httpContextAccessor.HttpContext.Session.SetString("Username", user.Username);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _httpContextAccessor.HttpContext.Session.Clear(); // 🔹 Xóa toàn bộ Session khi logout
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
