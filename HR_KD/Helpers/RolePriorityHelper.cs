using System.Security.Claims;

namespace HR_KD.Helpers
{
    public static class RolePriorityHelper
    {
        private static readonly List<string> RolePriority = new()
        {
            "DIRECTOR",
            "LINE_MANAGER",
            "EMPLOYEE_MANAGER",
            "EMPLOYEE"
        };

        public static string GetHighestRole(this ClaimsPrincipal user)
        {
            return RolePriority.FirstOrDefault(r => user.IsInRole(r)) ?? "EMPLOYEE";
        }

        public static bool IsAtLeast(this ClaimsPrincipal user, string role)
        {
            int userLevel = RolePriority.IndexOf(GetHighestRole(user));
            int requiredLevel = RolePriority.IndexOf(role);
            return userLevel <= requiredLevel;
        }
    }
}
