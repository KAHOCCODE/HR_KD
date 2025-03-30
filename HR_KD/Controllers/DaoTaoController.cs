using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class DaoTaoController : Controller
    {
        // GET: DaoTao/Index
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Index()
        {
            return View();
        }

        // GET: DaoTao/Details/5
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Details(int id)
        {
            return View();
        }

        // GET: DaoTao/Create
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Create()
        {
            return View();
        }

        // GET: DaoTao/Edit/5
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Edit(int id)
        {
            return View();
        }

        // GET: DaoTao/Delete/5
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Delete(int id)
        {
            return View();
        }

        // GET: DaoTao/Assign/5
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public IActionResult Assign(int id)
        {
            return View();
        }

        // GET: DaoTao/ViewTraining
        [Authorize(Roles = "EMPLOYEE")]
        public IActionResult ViewTraining()
        {
            return View();
        }
    }
}