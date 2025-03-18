using Microsoft.AspNetCore.Mvc;

namespace HR_KD.ApiControllers
{
    public class AttendanceRequestManagerApiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
