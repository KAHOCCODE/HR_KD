using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
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

        #region view bảng lương theo phòng ban / trạng thái chung
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region view report lương 
        [Authorize(Roles = "DIRECTOR,EMPLOYEE_MANAGER,LINE_MANAGER,PAYROLL_AUDITOR")]
        public IActionResult PayrollReport()
        {
            return View();
        }
        #endregion
    }
}