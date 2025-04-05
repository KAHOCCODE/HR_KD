using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HrDbContext _context;

        public EmployeesController(HrDbContext context)
        {
            _context = context;
        }

        //[Authorize(Roles = "EMPLOYEE")]
        public IActionResult Index()
        {
            return View();
        }
        //[Authorize(Roles = "EMPLOYEE_MANAGER")]
        // Trong NhanVienController.cs
        public async Task<IActionResult> Create(NhanVien nhanVien, ThongTinLuongNVDTO salaryDTO)
        {
            if (ModelState.IsValid)
            {
                _context.NhanViens.Add(nhanVien);
                await _context.SaveChangesAsync();

                // Gọi API để thiết lập lương
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://localhost:5012/api/"); // Thay bằng URL API của bạn
                    salaryDTO.MaNv = nhanVien.MaNv;
                    var response = await client.PostAsJsonAsync("SalaryApi/setup", salaryDTO);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Xử lý lỗi nếu không thiết lập được lương
                        ModelState.AddModelError("", "Không thể thiết lập lương cho nhân viên.");
                        return View(nhanVien);
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            return View(nhanVien);
        }
        public async Task<IActionResult> SetupSalary(int maNv)
        {
            var nhanVien = await _context.NhanViens.FindAsync(maNv);
            if (nhanVien == null)
            {
                return NotFound();
            }

            // Kiểm tra xem nhân viên đã có lương chưa
            var existingSalary = await _context.ThongTinLuongNVs
                .Where(s => s.MaNv == maNv)
                .OrderByDescending(s => s.NgayApDng)
                .FirstOrDefaultAsync();

            if (existingSalary != null)
            {
                return RedirectToAction("Index"); // Hoặc hiển thị thông báo rằng nhân viên đã có lương
            }

            ViewBag.MaNv = maNv;
            return View();
        }
    }
}
