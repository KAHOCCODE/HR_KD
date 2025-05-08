using HR_KD.Data;
using HR_KD.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "EMPLOYEE_MANAGER,LINE_MANAGER")]
    public class SalaryApiController : ControllerBase
    {
        private readonly HrDbContext _context;
        private readonly ILogger<SalaryApiController> _logger;
        private readonly PayrollCalculator _payrollCalculator;

        public SalaryApiController(HrDbContext context, ILogger<SalaryApiController> logger)
        {
            _context = context;
            _logger = logger;
            _payrollCalculator = new PayrollCalculator(context);
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
                if (salaryDTO.LuongCoBan < 0 || salaryDTO.PhuCapCoDinh < 0 || salaryDTO.ThuongCoDinh < 0)
                {
                    return BadRequest("Lương, phụ cấp, thưởng không được âm.");
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

                // Lấy thông tin mức lương tối thiểu vùng đang hoạt động
                var activeMinimumWage = await _context.MucLuongToiThieuVungs
                    .Where(m => m.IsActive)
                    .FirstOrDefaultAsync();

                if (activeMinimumWage == null)
                {
                    return BadRequest("Không tìm thấy mức lương tối thiểu vùng đang hoạt động.");
                }

                // Kiểm tra lương cơ bản so với mức lương tối thiểu
                var minimumWage = activeMinimumWage.MucLuongToiThieuThang;
                if (salaryDTO.LuongCoBan < minimumWage)
                {
                    return BadRequest($"Lương cơ bản phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {minimumWage:N0} VNĐ.");
                }

                // Kiểm tra tổng lương đóng bảo hiểm
                var luongDongBaoHiem = salaryDTO.LuongCoBan + (salaryDTO.PhuCapCoDinh ?? 0) + (salaryDTO.ThuongCoDinh ?? 0);
                if (luongDongBaoHiem < minimumWage)
                {
                    return BadRequest($"Tổng lương đóng bảo hiểm (lương cơ bản + phụ cấp + thưởng) phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {minimumWage:N0} VNĐ.");
                }

                // Tính toán các khoản bảo hiểm bằng PayrollCalculator
                (decimal bhxh, decimal bhyt, decimal bhtn, string errorMessage) = await _payrollCalculator.CalculateInsurance(
                    salaryDTO.LuongCoBan,
                    salaryDTO.PhuCapCoDinh,
                    salaryDTO.ThuongCoDinh
                );
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(errorMessage);
                }

                // Gán các khoản bảo hiểm
                salaryDTO.BHXH = bhxh;
                salaryDTO.BHYT = bhyt;
                salaryDTO.BHTN = bhtn;

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
                var existingSalary = await _context.ThongTinLuongNVs
                    .FirstOrDefaultAsync(s => s.MaLuongNV == maLuongNV);

                if (existingSalary == null)
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
                if (salaryDTO.LuongCoBan < 0 || salaryDTO.PhuCapCoDinh < 0 || salaryDTO.ThuongCoDinh < 0)
                {
                    return BadRequest("Lương, phụ cấp, thưởng không được âm.");
                }

                if (salaryDTO.NgayApDung < DateTime.Now.Date)
                {
                    return BadRequest("Ngày áp dụng không được là ngày trong quá khứ.");
                }

                // Kiểm tra xem có bản ghi lương nào khác với NgayApDung mới hơn hoặc bằng ngày áp dụng mới
                var futureSalary = await _context.ThongTinLuongNVs
                    .Where(s => s.MaNv == salaryDTO.MaNv && s.NgayApDung >= salaryDTO.NgayApDung && s.MaLuongNV != maLuongNV)
                    .FirstOrDefaultAsync();

                if (futureSalary != null)
                {
                    return BadRequest("Đã tồn tại thông tin lương với ngày áp dụng mới hơn hoặc bằng ngày này.");
                }

                // Lấy thông tin mức lương tối thiểu vùng đang hoạt động
                var activeMinimumWage = await _context.MucLuongToiThieuVungs
                    .Where(m => m.IsActive)
                    .FirstOrDefaultAsync();

                if (activeMinimumWage == null)
                {
                    return BadRequest("Không tìm thấy mức lương tối thiểu vùng đang hoạt động.");
                }

                // Kiểm tra lương cơ bản so với mức lương tối thiểu
                var minimumWage = activeMinimumWage.MucLuongToiThieuThang;
                if (salaryDTO.LuongCoBan < minimumWage)
                {
                    return BadRequest($"Lương cơ bản phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {minimumWage:N0} VNĐ.");
                }

                // Kiểm tra tổng lương đóng bảo hiểm
                var luongDongBaoHiem = salaryDTO.LuongCoBan + (salaryDTO.PhuCapCoDinh ?? 0) + (salaryDTO.ThuongCoDinh ?? 0);
                if (luongDongBaoHiem < minimumWage)
                {
                    return BadRequest($"Tổng lương đóng bảo hiểm (lương cơ bản + phụ cấp + thưởng) phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {minimumWage:N0} VNĐ.");
                }

                // Tính toán các khoản bảo hiểm bằng PayrollCalculator
                (decimal bhxh, decimal bhyt, decimal bhtn, string errorMessage) = await _payrollCalculator.CalculateInsurance(
                    salaryDTO.LuongCoBan,
                    salaryDTO.PhuCapCoDinh,
                    salaryDTO.ThuongCoDinh
                );
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(errorMessage);
                }

                // Gán các khoản bảo hiểm
                salaryDTO.BHXH = bhxh;
                salaryDTO.BHYT = bhyt;
                salaryDTO.BHTN = bhtn;

                // Tạo bản ghi lương mới
                var newSalary = new ThongTinLuongNV
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

                _context.ThongTinLuongNVs.Add(newSalary);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Đã cập nhật lương cho nhân viên MaNv: {MaNv}, MaLuongNV mới: {MaLuongNV}", newSalary.MaNv, newSalary.MaLuongNV);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật lương cho nhân viên MaNv: {MaNv}, MaLuongNV: {MaLuongNV}", salaryDTO.MaNv, maLuongNV);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật lương.");
            }
        }

        // POST: api/SalaryApi/calculate-insurance
        [HttpPost("calculate-insurance")]
        public async Task<ActionResult<InsuranceCalculationDTO>> CalculateInsurance([FromBody] InsuranceInputDTO input)
        {
            try
            {
                if (input.LuongCoBan < 0 || input.PhuCapCoDinh < 0 || input.ThuongCoDinh < 0)
                {
                    return BadRequest("Lương cơ bản, phụ cấp cố định và thưởng cố định không được âm.");
                }

                var (bhxh, bhyt, bhtn, errorMessage) = await _payrollCalculator.CalculateInsurance(
                    input.LuongCoBan,
                    input.PhuCapCoDinh,
                    input.ThuongCoDinh
                );
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return BadRequest(errorMessage);
                }

                var result = new InsuranceCalculationDTO
                {
                    BHXH = bhxh,
                    BHYT = bhyt,
                    BHTN = bhtn
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính toán bảo hiểm: LuongCoBan={LuongCoBan}, PhuCapCoDinh={PhuCapCoDinh}, ThuongCoDinh={ThuongCoDinh}",
                    input.LuongCoBan, input.PhuCapCoDinh, input.ThuongCoDinh);
                return StatusCode(500, "Có lỗi xảy ra khi tính toán bảo hiểm.");
            }
        }
    }

    public class InsuranceInputDTO
    {
        public decimal LuongCoBan { get; set; }
        public decimal? PhuCapCoDinh { get; set; }
        public decimal? ThuongCoDinh { get; set; }
    }

    public class InsuranceCalculationDTO
    {
        public decimal BHXH { get; set; }
        public decimal BHYT { get; set; }
        public decimal BHTN { get; set; }
    }
}