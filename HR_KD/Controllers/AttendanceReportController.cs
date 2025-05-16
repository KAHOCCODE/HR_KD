using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class AttendanceReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
