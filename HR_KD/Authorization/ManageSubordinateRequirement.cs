using Microsoft.AspNetCore.Authorization;

namespace HR_KD.Authorization
{
    public class ManageSubordinateRequirement : IAuthorizationRequirement
    {
        public string Module { get; }
        public string Action { get; }

        public ManageSubordinateRequirement(string module, string action)
        {
            Module = module;
            Action = action;
        }
    }
}