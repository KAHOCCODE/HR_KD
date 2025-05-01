using HR_KD.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class PayrollCalculator
{
    private readonly HrDbContext _context;

    public PayrollCalculator(HrDbContext context)
    {
        _context = context;
    }

    public async Task<BangLuong> CalculatePayroll(int employeeId, DateTime monthYear, List<ChamCong> attendanceRecords, ThongTinLuongNV salaryInfo)
    {
        var payroll = new BangLuong
        {
            MaNv = employeeId,
            ThangNam = DateOnly.FromDateTime(monthYear),
            TrangThai = "Đã tạo - Chưa thanh toán"
        };

        // 1. Lấy thông tin hợp đồng và tỷ lệ lương từ LoaiHopDong
        var contract = await _context.HopDongLaoDongs
            .Include(hd => hd.LoaiHopDong)
            .FirstOrDefaultAsync(hd => hd.MaNv == employeeId && hd.IsActive);

        if (contract == null)
        {
            throw new Exception("Nhân viên không có hợp đồng lao động hợp lệ.");
        }

        decimal tiLeLuong = (decimal)(contract.LoaiHopDong.TiLeLuong ?? 1.0);

        // 2. Tính tổng giờ làm việc
        decimal totalHours = 27 * 8; // Sửa cứng tạm thời: 27 ngày công * 8 giờ/ngày = 216 giờ
        // Nếu dữ liệu ChamCong đáng tin cậy, nên dùng:
        // decimal totalHours = attendanceRecords.Sum(a => a.TongGio ?? 0);

        // 3. Tính lương cơ bản theo giờ (giả định 160 giờ/tháng) và áp dụng tỷ lệ lương ngay từ đầu
        decimal adjustedSalary = salaryInfo.LuongCoBan * tiLeLuong;
        decimal hourlyRate = adjustedSalary / 160;
        decimal baseSalary = totalHours * hourlyRate;

        // 4. Tính lương tăng ca (lọc trạng thái "Đã duyệt lần 1")
        var overtimeRecords = await _context.TangCas
            .Where(t => t.MaNv == employeeId &&
                        t.NgayTangCa.Year == monthYear.Year &&
                        t.NgayTangCa.Month == monthYear.Month &&
                        t.TrangThai.Trim() == "Đã duyệt lần 1")
            .ToListAsync();
        decimal overtimeSalary = overtimeRecords.Sum(t => (decimal)t.SoGioTangCa * hourlyRate * t.TyLeTangCa);

        // 5. Tính tổng lương trước khấu trừ
        decimal grossSalary = baseSalary +
                             (salaryInfo.PhuCapCoDinh ?? 0) +
                             (salaryInfo.ThuongCoDinh ?? 0) +
                             overtimeSalary;

        // 6. Tính các khoản khấu trừ bảo hiểm
        decimal bhxh = salaryInfo.BHXH;
        decimal bhyt = salaryInfo.BHYT;
        decimal bhtn = salaryInfo.BHTN;

        // 7. Lấy thông tin giảm trừ gia cảnh
        var giamTruGiaCanh = await _context.GiamTruGiaCanhs
            .Where(g => g.NgayHieuLuc <= monthYear && (g.NgayHetHieuLuc == null || g.NgayHetHieuLuc >= monthYear))
            .FirstOrDefaultAsync();

        if (giamTruGiaCanh == null)
        {
            throw new Exception("Không tìm thấy thông tin giảm trừ gia cảnh hợp lệ.");
        }

        // 8. Lấy số lượng người phụ thuộc từ NhanVien
        var employee = await _context.NhanViens
            .FirstOrDefaultAsync(nv => nv.MaNv == employeeId);

        if (employee == null)
        {
            throw new Exception("Không tìm thấy thông tin nhân viên.");
        }

        decimal mucGiamTruBanThan = giamTruGiaCanh.MucGiamTruBanThan;
        decimal mucGiamTruNguoiPhuThuoc = giamTruGiaCanh.MucGiamTruNguoiPhuThuoc;
        int soNguoiPhuThuoc = employee.SoNguoiPhuThuoc;

        // 9. Tính thu nhập chịu thuế
        decimal giamTruTongCong = mucGiamTruBanThan + (soNguoiPhuThuoc * mucGiamTruNguoiPhuThuoc);
        decimal taxableIncome = grossSalary - bhxh - bhyt - bhtn - giamTruTongCong;

        // 10. Tính thuế TNCN (dùng bảng lũy tiến)
        decimal personalIncomeTax = 0;
        if (taxableIncome > 0)
        {
            personalIncomeTax = CalculatePersonalIncomeTax(taxableIncome);
        }

        // 11. Tính lương thực nhận
        decimal netSalary = grossSalary - bhxh - bhyt - bhtn - personalIncomeTax;

        // 12. Gán giá trị cho bảng lương
        payroll.TongLuong = grossSalary;
        payroll.LuongTangCa = overtimeSalary;
        payroll.PhuCapThem = 0;
        payroll.LuongThem = 0;
        payroll.ThueTNCN = personalIncomeTax;
        payroll.ThucNhan = netSalary;

        return payroll;
    }

    private decimal CalculatePersonalIncomeTax(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0;

        decimal tax = 0;
        if (taxableIncome <= 5_000_000)
            tax = taxableIncome * 0.05m;
        else if (taxableIncome <= 10_000_000)
            tax = 5_000_000 * 0.05m + (taxableIncome - 5_000_000) * 0.10m;
        else if (taxableIncome <= 18_000_000)
            tax = 5_000_000 * 0.05m + 5_000_000 * 0.10m + (taxableIncome - 10_000_000) * 0.15m;
        else if (taxableIncome <= 32_000_000)
            tax = 5_000_000 * 0.05m + 5_000_000 * 0.10m + 8_000_000 * 0.15m + (taxableIncome - 18_000_000) * 0.20m;
        else if (taxableIncome <= 52_000_000)
            tax = 5_000_000 * 0.05m + 5_000_000 * 0.10m + 8_000_000 * 0.15m + 14_000_000 * 0.20m + (taxableIncome - 32_000_000) * 0.25m;
        else if (taxableIncome <= 80_000_000)
            tax = 5_000_000 * 0.05m + 5_000_000 * 0.10m + 8_000_000 * 0.15m + 14_000_000 * 0.20m + 20_000_000 * 0.25m + (taxableIncome - 52_000_000) * 0.30m;
        else
            tax = 5_000_000 * 0.05m + 5_000_000 * 0.10m + 8_000_000 * 0.15m + 14_000_000 * 0.20m + 20_000_000 * 0.25m + 28_000_000 * 0.30m + (taxableIncome - 80_000_000) * 0.35m;

        return tax;
    }

    public async Task<(decimal BHXH, decimal BHYT, decimal BHTN, string ErrorMessage)> CalculateInsurance(decimal luongCoBan, decimal? phuCapCoDinh)
    {
        try
        {
            var activeMinimumWage = await _context.MucLuongToiThieuVungs
                .Where(m => m.IsActive)
                .FirstOrDefaultAsync();

            if (activeMinimumWage == null)
            {
                return (0, 0, 0, "Không tìm thấy mức lương tối thiểu vùng đang hoạt động.");
            }

            var activeBaseSalary = await _context.MucLuongCoSos
                .Where(m => m.IsActive)
                .FirstOrDefaultAsync();

            if (activeBaseSalary == null)
            {
                return (0, 0, 0, "Không tìm thấy mức lương cơ sở đang hoạt động.");
            }

            var insuranceRates = await _context.ThongTinBaoHiems
                .Where(b => b.NgayHetHieuLuc == null)
                .ToListAsync();

            var bhxhRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHXH")?.TyLeNguoiLaoDong ?? 0;
            var bhytRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHYT")?.TyLeNguoiLaoDong ?? 0;
            var bhtnRate = insuranceRates.FirstOrDefault(b => b.LoaiBaoHiem == "BHTN")?.TyLeNguoiLaoDong ?? 0;

            var luongDongBaoHiem = luongCoBan + (phuCapCoDinh ?? 0);

            var mucLuongToiThieuVung = activeMinimumWage.MucLuongToiThieuThang;
            var mucLuongToiDaBHXH_BHYT = activeBaseSalary.LuongCoSo * 20;
            var mucLuongToiDaBHTN = activeMinimumWage.MucLuongToiThieuThang * 20;

            if (luongDongBaoHiem < mucLuongToiThieuVung)
            {
                return (0, 0, 0, $"Mức lương đóng bảo hiểm (lương cơ bản + phụ cấp) phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {mucLuongToiThieuVung:N0} VNĐ.");
            }

            var luongDongBHXH = Math.Min(luongDongBaoHiem, mucLuongToiDaBHXH_BHYT);
            var luongDongBHYT = Math.Min(luongDongBaoHiem, mucLuongToiDaBHXH_BHYT);
            var luongDongBHTN = Math.Min(luongDongBaoHiem, mucLuongToiDaBHTN);

            var bhxh = luongDongBHXH * (bhxhRate / 100);
            var bhyt = luongDongBHYT * (bhytRate / 100);
            var bhtn = luongDongBHTN * (bhtnRate / 100);

            if (bhxh < 0 || bhyt < 0 || bhtn < 0)
            {
                return (0, 0, 0, "Các khoản bảo hiểm không được âm.");
            }

            return (bhxh, bhyt, bhtn, null);
        }
        catch (Exception ex)
        {
            return (0, 0, 0, $"Lỗi khi tính toán bảo hiểm: {ex.Message}");
        }
    }
}