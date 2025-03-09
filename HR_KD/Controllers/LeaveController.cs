using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class LeaveController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
