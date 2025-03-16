using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class HolidayController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
