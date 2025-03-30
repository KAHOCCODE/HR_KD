using HR_KD.Data;
using HR_KD.DTOs;
using HR_KD.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DaoTaoApiController : ControllerBase
    {
        private readonly HrDbContext _context;

        public DaoTaoApiController(HrDbContext context)
        {
            _context = context;
        }

        // GET: api/DaoTaoApi
        [HttpGet]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
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
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
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

        // GET: api/DaoTaoApi/5/lichsu
        [HttpGet("{id}/lichsu")]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<ActionResult<IEnumerable<LichSuDaoTaoDTO>>> GetLichSuDaoTao(int id)
        {
            var lichSuDaoTaos = await _context.LichSuDaoTaos
                .Include(ls => ls.MaNvNavigation)
                .Where(ls => ls.MaDaoTao == id)
                .Select(ls => new LichSuDaoTaoDTO
                {
                    MaLichSu = ls.MaLichSu,
                    MaNv = ls.MaNv,
                    HoTen = ls.MaNvNavigation.HoTen,
                    MaDaoTao = ls.MaDaoTao,
                    KetQua = ls.KetQua
                })
                .ToListAsync();
            return Ok(lichSuDaoTaos);
        }

        // POST: api/DaoTaoApi
        [HttpPost]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<ActionResult<DaoTaoDTO>> CreateDaoTao(DaoTaoDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

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
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> EditDaoTao(int id, DaoTaoDTO dto)
        {
            if (id != dto.MaDaoTao) return BadRequest();

            var daoTao = await _context.DaoTaos
                .Include(d => d.LichSuDaoTaos)
                .FirstOrDefaultAsync(d => d.MaDaoTao == id);

            if (daoTao == null) return NotFound();

            if (daoTao.MaPhongBan != dto.MaPhongBan)
            {
                _context.LichSuDaoTaos.RemoveRange(daoTao.LichSuDaoTaos);
            }

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
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> DeleteDaoTao(int id)
        {
            var daoTao = await _context.DaoTaos.FindAsync(id);
            if (daoTao == null) return NotFound();

            _context.DaoTaos.Remove(daoTao);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/DaoTaoApi/assign/5
        [HttpGet("assign/{id}")]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<ActionResult<IEnumerable<NhanVien>>> GetEmployeesForAssign(int id)
        {
            var daoTao = await _context.DaoTaos.FindAsync(id);
            if (daoTao == null) return NotFound();

            var assignedMaNvs = _context.LichSuDaoTaos
                .Where(ls => ls.MaDaoTao == id)
                .Select(ls => ls.MaNv)
                .ToList();

            var employees = await _context.NhanViens
                .Where(nv => nv.MaPhongBan == daoTao.MaPhongBan && !assignedMaNvs.Contains(nv.MaNv))
                .ToListAsync();

            return Ok(employees);
        }

        
        [HttpPost("assign")]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> AssignEmployees([FromBody] AssignEmployeeModel model)
        {
            var daoTao = await _context.DaoTaos.FindAsync(model.MaDaoTao);
            if (daoTao == null) return NotFound();

            var employees = await _context.NhanViens
                .Where(nv => model.MaNvs.Contains(nv.MaNv) && nv.MaPhongBan == daoTao.MaPhongBan)
                .ToListAsync();

            if (employees.Count != model.MaNvs.Count) return BadRequest();

            foreach (var maNv in model.MaNvs)
            {
                if (!_context.LichSuDaoTaos.Any(ls => ls.MaNv == maNv && ls.MaDaoTao == model.MaDaoTao))
                {
                    _context.LichSuDaoTaos.Add(new LichSuDaoTao
                    {
                        MaNv = maNv,
                        MaDaoTao = model.MaDaoTao,
                        KetQua = "Chưa Hoàn Thành"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }


        // GET: api/DaoTaoApi/viewtraining
        [HttpGet("viewtraining")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<ActionResult> ViewTraining()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName)) return Forbid();

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.HoTen == userName);

            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");

            int maNv = nhanVien.MaNv;

            var lichSuDaoTaos = await _context.LichSuDaoTaos
                .Include(ls => ls.MaDaoTaoNavigation)
                .ThenInclude(d => d.PhongBan)
                .Where(ls => ls.MaNv == maNv)
                .Select(ls => new
                {
                    DaoTao = new DaoTaoDTO
                    {
                        MaDaoTao = ls.MaDaoTaoNavigation.MaDaoTao,
                        TenDaoTao = ls.MaDaoTaoNavigation.TenDaoTao,
                        MoTa = ls.MaDaoTaoNavigation.MoTa,
                        NoiDung = ls.MaDaoTaoNavigation.NoiDung,
                        NgayBatDau = ls.MaDaoTaoNavigation.NgayBatDau,
                        NgayKetThuc = ls.MaDaoTaoNavigation.NgayKetThuc,
                        MaPhongBan = ls.MaDaoTaoNavigation.MaPhongBan,
                        TenPhongBan = ls.MaDaoTaoNavigation.PhongBan.TenPhongBan
                    },
                    LichSu = new LichSuDaoTaoDTO
                    {
                        MaLichSu = ls.MaLichSu,
                        MaNv = ls.MaNv,
                        MaDaoTao = ls.MaDaoTao,
                        KetQua = ls.KetQua
                    }
                })
                .ToListAsync();

            return Ok(lichSuDaoTaos);
        }

        [HttpPost("unassign/{maLichSu}/{maDaoTao}")]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> UnassignEmployee(int maLichSu, int maDaoTao)
        {
            var lichSu = await _context.LichSuDaoTaos.FindAsync(maLichSu);
            if (lichSu == null || lichSu.MaDaoTao != maDaoTao) return NotFound();

            _context.LichSuDaoTaos.Remove(lichSu);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("unassignall/{maDaoTao}")]
        [Authorize(Roles = "EMPLOYEE_MANAGER")]
        public async Task<IActionResult> UnassignAllEmployees(int maDaoTao)
        {
            var daoTao = await _context.DaoTaos
                .Include(d => d.LichSuDaoTaos)
                .FirstOrDefaultAsync(d => d.MaDaoTao == maDaoTao);

            if (daoTao == null) return NotFound();

            _context.LichSuDaoTaos.RemoveRange(daoTao.LichSuDaoTaos);
            await _context.SaveChangesAsync();
            return Ok();
        }

        

        [HttpPost("completetraining")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> CompleteTraining([FromBody] CompleteTrainingDTO model)
        {
            var lichSu = await _context.LichSuDaoTaos.FindAsync(model.MaLichSu);
            if (lichSu == null) return NotFound();

            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName)) return Forbid();

            var nhanVien = await _context.NhanViens
                .FirstOrDefaultAsync(nv => nv.HoTen == userName);

            if (nhanVien == null) return NotFound("Không tìm thấy nhân viên.");
            if (lichSu.MaNv != nhanVien.MaNv) return Forbid();

            lichSu.KetQua = "Hoàn Thành";
            await _context.SaveChangesAsync();
            return Ok();
        }

    }
}