using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhongBanApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public PhongBanApiController(HrDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetPhongBans")]
        public IActionResult GetPhongBans()
        {
            var phongBans = _context.PhongBans
                .Select(pb => new { pb.MaPhongBan, pb.TenPhongBan })
                .ToList();

            return Ok(phongBans);
        }
    }
}
