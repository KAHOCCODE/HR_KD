using HR_KD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoaiHopDongApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public LoaiHopDongApiController(HrDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetLoaiHopDongs()
        {
            var loaiHopDongs = _context.LoaiHopDongs
                .Where(l => l.IsActive)
                .Select(l => new
                {
                    l.MaLoaiHopDong,
                    l.TenLoaiHopDong,
                    l.MoTa,
                    l.ThoiHanMacDinh
                })
                .ToList();
            return Ok(loaiHopDongs);
        }
    }
}