using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DaoTaoApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public DaoTaoApiController(HrDbContext context)
        {
            _context = context;
        }

        // GET: api/DaoTaoApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DaoTaoDTO>>> GetDaoTaos()
        {
            var daoTaos = await _context.DaoTaos
                .Include(d => d.PhongBan)
                .Select(d => new DaoTaoDTO
                {
                    MaDaoTao = d.MaDaoTao,
                    TenDaoTao = d.TenDaoTao,
                    MoTa = d.MoTa,
                    NoiDung = d.NoiDung,
                    NgayBatDau = d.NgayBatDau,
                    NgayKetThuc = d.NgayKetThuc,
                    MaPhongBan = d.MaPhongBan,
                    TenPhongBan = d.PhongBan.TenPhongBan
                })
                .ToListAsync();
            return Ok(daoTaos);
        }

        // GET: api/DaoTaoApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DaoTaoDTO>> GetDaoTao(int id)
        {
            var daoTao = await _context.DaoTaos
                .Include(d => d.PhongBan)
                .Select(d => new DaoTaoDTO
                {
                    MaDaoTao = d.MaDaoTao,
                    TenDaoTao = d.TenDaoTao,
                    MoTa = d.MoTa,
                    NoiDung = d.NoiDung,
                    NgayBatDau = d.NgayBatDau,
                    NgayKetThuc = d.NgayKetThuc,
                    MaPhongBan = d.MaPhongBan,
                    TenPhongBan = d.PhongBan.TenPhongBan
                })
                .FirstOrDefaultAsync(d => d.MaDaoTao == id);

            if (daoTao == null) return NotFound();
            return Ok(daoTao);
        }

        // POST: api/DaoTaoApi
        [HttpPost]
        public async Task<ActionResult<DaoTaoDTO>> CreateDaoTao(DaoTaoDTO dto)
        {
            var daoTao = new DaoTao
            {
                TenDaoTao = dto.TenDaoTao,
                MoTa = dto.MoTa,
                NoiDung = dto.NoiDung,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc,
                MaPhongBan = dto.MaPhongBan
            };

            _context.DaoTaos.Add(daoTao);
            await _context.SaveChangesAsync();

            dto.MaDaoTao = daoTao.MaDaoTao;
            return CreatedAtAction(nameof(GetDaoTao), new { id = daoTao.MaDaoTao }, dto);
        }

        // PUT: api/DaoTaoApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDaoTao(int id, DaoTaoDTO dto)
        {
            if (id != dto.MaDaoTao) return BadRequest();

            var daoTao = await _context.DaoTaos.FindAsync(id);
            if (daoTao == null) return NotFound();

            daoTao.TenDaoTao = dto.TenDaoTao;
            daoTao.MoTa = dto.MoTa;
            daoTao.NoiDung = dto.NoiDung;
            daoTao.NgayBatDau = dto.NgayBatDau;
            daoTao.NgayKetThuc = dto.NgayKetThuc;
            daoTao.MaPhongBan = dto.MaPhongBan;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/DaoTaoApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDaoTao(int id)
        {
            var daoTao = await _context.DaoTaos.FindAsync(id);
            if (daoTao == null) return NotFound();

            _context.DaoTaos.Remove(daoTao);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/DaoTaoApi/assign
        [HttpPost("assign")]
        public async Task<IActionResult> AssignEmployees(AssignEmployeesDTO dto)
        {
            var daoTao = await _context.DaoTaos.FindAsync(dto.MaDaoTao);
            if (daoTao == null) return NotFound("Khóa đào tạo không tồn tại");

            var employees = await _context.NhanViens
                .Where(nv => dto.MaNvs.Contains(nv.MaNv) && nv.MaPhongBan == daoTao.MaPhongBan)
                .ToListAsync();

            if (employees.Count != dto.MaNvs.Count)
                return BadRequest("Một số nhân viên không thuộc phòng ban của khóa đào tạo");

            foreach (var maNv in dto.MaNvs)
            {
                if (!_context.LichSuDaoTaos.Any(ls => ls.MaNv == maNv && ls.MaDaoTao == dto.MaDaoTao))
                {
                    _context.LichSuDaoTaos.Add(new LichSuDaoTao
                    {
                        MaNv = maNv,
                        MaDaoTao = dto.MaDaoTao,
                        KetQua = "Chưa Hoàn Thành"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/DaoTaoApi/complete/5
        [HttpPut("complete/{maLichSu}")]
        public async Task<IActionResult> CompleteTraining(int maLichSu)
        {
            var lichSu = await _context.LichSuDaoTaos.FindAsync(maLichSu);
            if (lichSu == null) return NotFound();

            lichSu.KetQua = "Hoàn Thành";
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}