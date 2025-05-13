using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class AttendanceManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Director()
        {
            return View();
        }
        public IActionResult Manager()
        {
            return View();
        }
    }

}
