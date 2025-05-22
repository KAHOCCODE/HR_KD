using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Controllers
{
    [Authorize(Roles = "DIRECTOR")]
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