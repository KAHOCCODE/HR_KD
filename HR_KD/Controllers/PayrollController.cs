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

        #region view bảng lương theo phòng ban 
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region view bảng lương nhân viên 
        [Authorize(Roles = "EMPLOYEE")]
        public IActionResult MyPayroll()
        {
            return View();
        }
        #endregion

    }
}