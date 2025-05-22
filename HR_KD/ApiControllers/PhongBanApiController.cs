using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhongBanApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly ILogger<PhongBanApiController> _logger; // Add this field

        public PhongBanApiController(HrDbContext context, ILogger<PhongBanApiController> logger) // Update constructor
        {
            _context = context;
            _logger = logger; // Initialize the logger
        }

        // GET: api/PhongBanApi
        [HttpGet]
        public async Task<IActionResult> GetPhongBans()
        {
            try
            {
                var phongBans = await _context.PhongBans
                    .Select(pb => new
                    {
                        pb.MaPhongBan,
                        pb.TenPhongBan
                    })
                    .ToListAsync();

                if (!phongBans.Any())
                {
                    return NotFound(new { message = "Không tìm thấy phòng ban nào." });
                }

                return Ok(phongBans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách phòng ban."); // Use the logger
                return StatusCode(500, new { message = "Lỗi server. Xem log để biết chi tiết.", error = ex.Message });
            }
        }
    }
}
