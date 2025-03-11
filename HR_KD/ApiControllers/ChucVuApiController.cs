using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChucVuApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public ChucVuApiController(HrDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách chức vụ
        [HttpGet]
        public IActionResult GetChucVus()
        {
            var chucVus = _context.ChucVus
                .Select(cv => new {cv.MaChucVu, cv.TenChucVu})
                .ToList();

            return Ok(chucVus);
        }
    }
}
