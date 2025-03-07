using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class AttendanceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

}
