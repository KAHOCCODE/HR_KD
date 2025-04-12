using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HR_KD.Authorization
{
    public class ManageSubordinateHandler : AuthorizationHandler<ManageSubordinateRequirement>
    {
        private readonly HrDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ManageSubordinateHandler(HrDbContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _httpContextAccessor = accessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageSubordinateRequirement requirement)
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Task.CompletedTask;

            var current = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .FirstOrDefault(t => t.Username == username);

            if (current == null) return Task.CompletedTask;

            var nv = current.MaNvNavigation;

            var hasSubordinates = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .Any(t =>
                    t.MaNvNavigation.MaPhongBan == nv.MaPhongBan &&
                    t.MaNvNavigation.MaChucVu > nv.MaChucVu);

            if (hasSubordinates)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
