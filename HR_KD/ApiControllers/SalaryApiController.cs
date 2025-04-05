using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "EMPLOYEE_MANAGER,LINE_MANAGER")]
    public class SalaryApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly ILogger<SalaryApiController> _logger;

        public SalaryApiController(HrDbContext context, ILogger<SalaryApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/SalaryApi/{maNv}
        [HttpGet("{maNv}")]
        public async Task<ActionResult<ThongTinLuongNVDTO>> GetSalary(int maNv)
        {
            var salary = await _context.ThongTinLuongNVs
                .Where(s => s.MaNv == maNv)
                .OrderByDescending(s => s.NgayApDng)
                .FirstOrDefaultAsync();

            if (salary == null)
            {
                return NotFound();
            }

            var salaryDTO = new ThongTinLuongNVDTO
            {
                MaLuongNV = salary.MaLuongNV,
                MaNv = salary.MaNv,
                LuongCoBan = salary.LuongCoBan,
                PhuCapCoDinh = salary.PhuCapCoDinh,
                ThuongCoDinh = salary.ThuongCoDinh,
                BHXH = salary.BHXH,
                BHYT = salary.BHYT,
                BHTN = salary.BHTN,
                NgayApDng = salary.NgayApDng,
                GhiChu = salary.GhiChu
            };

            return Ok(salaryDTO);
        }

        // POST: api/SalaryApi/setup
        [HttpPost("setup")]
        public async Task<ActionResult<ThongTinLuongNVDTO>> SetupSalary(ThongTinLuongNVDTO salaryDTO)
        {
            try
            {
                // Kiểm tra nhân viên tồn tại
                var nhanVien = await _context.NhanViens.FindAsync(salaryDTO.MaNv);
                if (nhanVien == null)
                {
                    return BadRequest("Nhân viên không tồn tại.");
                }

                // Kiểm tra dữ liệu đầu vào
                if (salaryDTO.LuongCoBan < 0 || salaryDTO.BHXH < 0 || salaryDTO.BHYT < 0 || salaryDTO.BHTN < 0)
                {
                    return BadRequest("Lương và các khoản bảo hiểm không được âm.");
                }

                if (salaryDTO.NgayApDng < DateTime.Now.Date)
                {
                    return BadRequest("Ngày áp dụng không được là ngày trong quá khứ.");
                }

                // Kiểm tra xem nhân viên đã có lương chưa
                var existingSalary = await _context.ThongTinLuongNVs
                    .Where(s => s.MaNv == salaryDTO.MaNv)
                    .OrderByDescending(s => s.NgayApDng)
                    .FirstOrDefaultAsync();

                if (existingSalary != null)
                {
                    return BadRequest("Nhân viên đã có thông tin lương.");
                }

                // Tạo bản ghi lương mới
                var salary = new ThongTinLuongNV
                {
                    MaNv = salaryDTO.MaNv,
                    LuongCoBan = salaryDTO.LuongCoBan,
                    PhuCapCoDinh = salaryDTO.PhuCapCoDinh,
                    ThuongCoDinh = salaryDTO.ThuongCoDinh,
                    BHXH = salaryDTO.BHXH,
                    BHYT = salaryDTO.BHYT,
                    BHTN = salaryDTO.BHTN,
                    NgayApDng = salaryDTO.NgayApDng,
                    GhiChu = salaryDTO.GhiChu
                };

                _context.ThongTinLuongNVs.Add(salary);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã thiết lập lương cho nhân viên MaNv: {MaNv}", salary.MaNv);

                salaryDTO.MaLuongNV = salary.MaLuongNV;
                return CreatedAtAction(nameof(GetSalary), new { maNv = salary.MaNv }, salaryDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thiết lập lương cho nhân viên MaNv: {MaNv}", salaryDTO.MaNv);
                return StatusCode(500, "Có lỗi xảy ra khi thiết lập lương.");
            }
        }
    }
}