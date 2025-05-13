using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class PayrollAccountantController : Controller
    {
        public IActionResult ByAccountant()
        {
            return View();
        }
    }
}