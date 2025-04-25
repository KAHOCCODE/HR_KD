using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class HolidayManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        
    }
}
