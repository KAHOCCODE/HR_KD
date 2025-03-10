using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class EmployeesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
