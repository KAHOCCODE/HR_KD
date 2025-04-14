using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class PayrollController : Controller
    {
        private readonly HrDbContext _context;

        public PayrollController(HrDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}