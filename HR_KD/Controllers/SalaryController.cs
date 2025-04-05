using HR_KD.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.Controllers
{
    public class SalaryController : Controller
    {
        private readonly HrDbContext _context;

        public SalaryController(HrDbContext context)
        {
            _context = context;
        }
    }
}