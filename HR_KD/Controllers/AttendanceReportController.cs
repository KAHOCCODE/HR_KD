using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class AttendanceReportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        //chỉ Director mới có quyền truy cập vào trang này

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
