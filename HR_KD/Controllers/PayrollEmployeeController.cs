using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    [Authorize(Roles = "EMPLOYEE")] // Only users with the "Employee" role can access this controller
    public class PayrollEmployeeController : Controller
    {
        // Dependency Injection (DbContext, etc.) might be needed here if your View requires server-side data before the client-side JS loads,
        // but for simply returning a View that uses client-side AJAX, it's often not needed.

        // Assuming MyPayroll view is for employees to see their own payroll
        public IActionResult MyPayroll()
        {
            return View();
        }

        // You might need other actions here if employees have other payroll-related views
        // E.g., public IActionResult PayrollDetail(int id) { return View(); }
    }
}