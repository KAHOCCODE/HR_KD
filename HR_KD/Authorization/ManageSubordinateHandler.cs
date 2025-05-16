using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HR_KD.Authorization
{
    public class ManageSubordinateHandler : AuthorizationHandler<ManageSubordinateRequirement, string>
    {
        private readonly HrDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ManageSubordinateHandler(HrDbContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _httpContextAccessor = accessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageSubordinateRequirement requirement, string module)
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return Task.CompletedTask;

            var current = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .Include(t => t.TaiKhoanQuyenHans).ThenInclude(t => t.QuyenHan)
                .FirstOrDefault(t => t.Username == username);

            if (current == null) return Task.CompletedTask;

            var nv = current.MaNvNavigation;

            // Kiểm tra quyền chi tiết cho module
            bool hasPermission = current.TaiKhoanQuyenHans
                .Select(t => t.QuyenHan.GetPermissions())
                .Any(permissions => permissions.ContainsKey(module) && permissions[module]["view"]);

            // Kiểm tra cấp dưới
            var hasSubordinates = _context.TaiKhoans
                .Include(t => t.MaNvNavigation)
                .Any(t =>
                    t.MaNvNavigation.MaPhongBan == nv.MaPhongBan &&
                    t.MaNvNavigation.MaChucVu > nv.MaChucVu);

            if (hasPermission && hasSubordinates)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}