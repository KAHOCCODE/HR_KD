﻿using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class AttendanceRequestManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
