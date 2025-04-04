using System.Security.Claims;

namespace HR_KD.Helpers
{
    public static class RoleHelper
    {
        public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
        {
            return roles.Any(role => user.IsInRole(role));
        }
    }
}
