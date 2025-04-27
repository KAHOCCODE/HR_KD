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
                .OrderByDescending(s => s.NgayApDung)
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
                NgayApDung = salary.NgayApDung,
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
                if (salaryDTO.LuongCoBan < 0 || salaryDTO.PhuCapCoDinh < 0 || salaryDTO.ThuongCoDinh < 0 ||
                    salaryDTO.BHXH < 0 || salaryDTO.BHYT < 0 || salaryDTO.BHTN < 0)
                {
                    return BadRequest("Lương, phụ cấp, thưởng và các khoản bảo hiểm không được âm.");
                }

                if (salaryDTO.NgayApDung < DateTime.Now.Date)
                {
                    return BadRequest("Ngày áp dụng không được là ngày trong quá khứ.");
                }

                // Kiểm tra xem nhân viên đã có lương chưa
                var existingSalary = await _context.ThongTinLuongNVs
                    .Where(s => s.MaNv == salaryDTO.MaNv)
                    .OrderByDescending(s => s.NgayApDung)
                    .FirstOrDefaultAsync();

                if (existingSalary != null)
                {
                    return BadRequest("Nhân viên đã có thông tin lương.");
                }

                // Lấy thông tin vùng lương đang hoạt động
                var activeRegion = await _context.VungLuongTheoDiaPhuongs
                    .Where(v => v.IsActive)
                    .FirstOrDefaultAsync();

                if (activeRegion == null)
                {
                    return BadRequest("Không tìm thấy vùng lương đang hoạt động.");
                }

                // Lấy mức lương tối thiểu của vùng
                var minimumWage = await _context.MucLuongToiThieuVungs
                    .Where(m => m.VungLuong == activeRegion.VungLuong)
                    .Select(m => m.MucLuongToiThieuThang)
                    .FirstOrDefaultAsync();

                if (minimumWage == 0)
                {
                    return BadRequest("Không tìm thấy mức lương tối thiểu cho vùng lương.");
                }

                // Kiểm tra lương cơ bản so với mức lương tối thiểu
                if (salaryDTO.LuongCoBan < minimumWage)
                {
                    return BadRequest($"Lương cơ bản phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {minimumWage:N0} VNĐ.");
                }

                // Lấy tỷ lệ bảo hiểm từ ThongTinBaoHiem
                var insuranceRates = await _context.ThongTinBaoHiems
                    .Where(b => b.NgayHetHieuLuc == null)
                    .ToListAsync();

                var bhxhRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHXH")?.TyLeNguoiLaoDong ?? 0;
                var bhytRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHYT")?.TyLeNguoiLaoDong ?? 0;
                var bhtnRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHTN")?.TyLeNguoiLaoDong ?? 0;

                // Tính toán các khoản bảo hiểm
                salaryDTO.BHXH = salaryDTO.LuongCoBan * (bhxhRate / 100);
                salaryDTO.BHYT = salaryDTO.LuongCoBan * (bhytRate / 100);
                salaryDTO.BHTN = salaryDTO.LuongCoBan * (bhtnRate / 100);

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
                    NgayApDung = salaryDTO.NgayApDung,
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

        // PUT: api/SalaryApi/edit/{maLuongNV}
        [HttpPut("edit/{maLuongNV}")]
        public async Task<ActionResult> EditSalary(int maLuongNV, ThongTinLuongNVDTO salaryDTO)
        {
            try
            {
                // Kiểm tra bản ghi lương tồn tại
                var salary = await _context.ThongTinLuongNVs
                    .FirstOrDefaultAsync(s => s.MaLuongNV == maLuongNV);

                if (salary == null)
                {
                    return NotFound("Không tìm thấy thông tin lương.");
                }

                // Kiểm tra nhân viên tồn tại
                var nhanVien = await _context.NhanViens.FindAsync(salaryDTO.MaNv);
                if (nhanVien == null)
                {
                    return BadRequest("Nhân viên không tồn tại.");
                }

                // Kiểm tra dữ liệu đầu vào
                if (salaryDTO.LuongCoBan < 0 || salaryDTO.PhuCapCoDinh < 0 || salaryDTO.ThuongCoDinh < 0 ||
                    salaryDTO.BHXH < 0 || salaryDTO.BHYT < 0 || salaryDTO.BHTN < 0)
                {
                    return BadRequest("Lương, phụ cấp, thưởng và các khoản bảo hiểm không được âm.");
                }

                if (salaryDTO.NgayApDung < DateTime.Now.Date)
                {
                    return BadRequest("Ngày áp dụng không được là ngày trong quá khứ.");
                }

                // Lấy thông tin vùng lương đang hoạt động
                var activeRegion = await _context.VungLuongTheoDiaPhuongs
                    .Where(v => v.IsActive)
                    .FirstOrDefaultAsync();

                if (activeRegion == null)
                {
                    return BadRequest("Không tìm thấy vùng lương đang hoạt động.");
                }

                // Lấy mức lương tối thiểu của vùng
                var minimumWage = await _context.MucLuongToiThieuVungs
                    .Where(m => m.VungLuong == activeRegion.VungLuong)
                    .Select(m => m.MucLuongToiThieuThang)
                    .FirstOrDefaultAsync();

                if (minimumWage == 0)
                {
                    return BadRequest("Không tìm thấy mức lương tối thiểu cho vùng lương.");
                }

                // Kiểm tra lương cơ bản so với mức lương tối thiểu
                if (salaryDTO.LuongCoBan < minimumWage)
                {
                    return BadRequest($"Lương cơ bản phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {minimumWage:N0} VNĐ.");
                }

                // Lấy tỷ lệ bảo hiểm từ ThongTinBaoHiem
                var insuranceRates = await _context.ThongTinBaoHiems
                    .Where(b => b.NgayHetHieuLuc == null)
                    .ToListAsync();

                var bhxhRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHXH")?.TyLeNguoiLaoDong ?? 0;
                var bhytRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHYT")?.TyLeNguoiLaoDong ?? 0;
                var bhtnRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHTN")?.TyLeNguoiLaoDong ?? 0;

                // Tính toán các khoản bảo hiểm
                salaryDTO.BHXH = salaryDTO.LuongCoBan * (bhxhRate / 100);
                salaryDTO.BHYT = salaryDTO.LuongCoBan * (bhytRate / 100);
                salaryDTO.BHTN = salaryDTO.LuongCoBan * (bhtnRate / 100);

                // Cập nhật thông tin lương
                salary.LuongCoBan = salaryDTO.LuongCoBan;
                salary.PhuCapCoDinh = salaryDTO.PhuCapCoDinh;
                salary.ThuongCoDinh = salaryDTO.ThuongCoDinh;
                salary.BHXH = salaryDTO.BHXH;
                salary.BHYT = salaryDTO.BHYT;
                salary.BHTN = salaryDTO.BHTN;
                salary.NgayApDung = salaryDTO.NgayApDung;
                salary.GhiChu = salaryDTO.GhiChu;

                _context.ThongTinLuongNVs.Update(salary);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã cập nhật lương cho nhân viên MaNv: {MaNv}, MaLuongNV: {MaLuongNV}", salary.MaNv, maLuongNV);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật lương cho nhân viên MaNv: {MaNv}, MaLuongNV: {MaLuongNV}", salaryDTO.MaNv, maLuongNV);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật lương.");
            }
        }
    }
}