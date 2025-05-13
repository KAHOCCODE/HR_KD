using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class PayrollManagerController : Controller
    {
        public IActionResult DepartmentReview()
        {
            return View();
        }
    }
}