using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class LeaveManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
