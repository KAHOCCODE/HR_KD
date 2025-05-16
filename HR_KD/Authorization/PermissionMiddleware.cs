using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.Authorization
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;

        public PermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, HrDbContext dbContext)
        {
            var endpoint = context.GetEndpoint();
            var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();

            if (authorizeAttribute != null)
            {
                var username = context.User.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: No user identity found.");
                    return;
                }

                var taiKhoan = await dbContext.TaiKhoans
                    .Include(t => t.TaiKhoanQuyenHans)
                    .ThenInclude(t => t.QuyenHan)
                    .FirstOrDefaultAsync(t => t.Username == username);

                if (taiKhoan == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized: User account not found.");
                    return;
                }

                var policy = authorizeAttribute.Policy;
                if (!string.IsNullOrEmpty(policy))
                {
                    var (module, action) = policy switch
                    {
                        "EmployeeView" => ("employees", "view"),
                        "EmployeeCreate" => ("employees", "create"),
                        "RoleManage" => ("employees", "assign_roles"),
                        "AttendanceManage" => ("attendance", "manage"),
                        "HolidayCreate" => ("holidays", "create"),
                        "HolidayApprove" => ("holidays", "approve"),
                        "PayrollCreate" => ("payroll", "create"),
                        "PayrollApproveBL2" => ("payroll", "approve_bl2_to_bl3"),
                        "PayrollApproveBL3" => ("payroll", "approve_bl3_to_bl4"),
                        _ => (null, null)
                    };

                    if (module != null && action != null)
                    {
                        bool hasPermission = taiKhoan.TaiKhoanQuyenHans
                            .Select(t => t.QuyenHan.GetPermissions())
                            .Any(permissions => permissions.ContainsKey(module) && permissions[module].ContainsKey(action) && permissions[module][action]);

                        if (!hasPermission)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsync($"Forbidden: User lacks {action} permission for {module}.");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}