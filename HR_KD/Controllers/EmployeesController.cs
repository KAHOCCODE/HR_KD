using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class EmployeesController : Controller
    {
        [Authorize(Roles = "EMPLOYEE")]
        public IActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Create()
        {
            return View();
        }
    }
}
