using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    [Authorize]
    public class PayrollApprovalController : Controller
    {
        public IActionResult DepartmentReview()
        {
            return View();
        }

        public IActionResult ByAccountant()
        {
            return View();
        }

        public IActionResult SendToDirector()
        {
            return View();
        }
    }
}
