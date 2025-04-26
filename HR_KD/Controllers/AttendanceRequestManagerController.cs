using HR_KD.DTOs;
using Microsoft.AspNetCore.Mvc;
using static AttendanceRequestManagerApiController;

namespace HR_KD.Controllers
{
    public class AttendanceRequestManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult SendReminderEmails()
        {
            return View(new SendReminderEmailsDTO());
        }
    }
}
