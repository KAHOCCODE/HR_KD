using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class HolidayController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult HolidayManagement()
        {
            return View();
        }
        public IActionResult Holiday()
        {
            return View();
        }
    }

}
