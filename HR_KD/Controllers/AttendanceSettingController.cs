using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HR_KD.Data;

namespace HR_KD.Controllers
{
    public class AttendanceSettingController : Controller
    {
        private readonly HrDbContext _context; // Replace with your actual DbContext

        public AttendanceSettingController(HrDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }


    }
}