using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    [Authorize(Roles = "DIRECTOR")]
    public class PayrollSettingController : Controller
    {
        // GET: /Setting/Index
        public IActionResult Index()
        {
            return View();
        }
    }
}