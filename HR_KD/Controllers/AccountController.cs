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
            if (!ModelState.IsValid) return View(model);

            var user = _context.TaiKhoans
                .Include(t => t.TaiKhoanQuyenHans)
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

            // ✅ Load và add nhiều Role Claims
            var userRoles = user.TaiKhoanQuyenHans.Select(q => q.MaQuyenHan).ToList();
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, nhanVien.HoTen),
        new Claim("MaNV", user.MaNv.ToString())
    };
            claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            // ✅ Đăng nhập & tạo session
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            _httpContextAccessor.HttpContext.Session.SetString("HoTen", nhanVien.HoTen);

            // ✅ Log đăng nhập
            var loginLog = new LoginHistory
            {
                LoginId = Guid.NewGuid(),
                UserId = user.MaNv.ToString(),
                Username = user.Username,
                Roles = string.Join(",", userRoles),
                LoginTime = DateTime.Now,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"]
            };
            _context.LoginHistories.Add(loginLog);
            await _context.SaveChangesAsync();

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
