using HR_KD.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChamCongApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public ChamCongApiController(HrDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetByEmployeeAndMonth")]
        public IActionResult GetChamCongByMonth(int maNv, int year, int month)
        {
            var chamCongs = _context.ChamCongs
                .Where(c => c.MaNv == maNv &&
                            c.NgayLamViec.Year == year &&
                            c.NgayLamViec.Month == month)
                .ToList();

            return Ok(chamCongs);
        }
    }
}
