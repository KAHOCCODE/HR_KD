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
            TrangThai = "BL1"
        };

        var contract = await _context.HopDongLaoDongs
            .Include(hd => hd.LoaiHopDong)
            .FirstOrDefaultAsync(hd => hd.MaNv == employeeId && hd.IsActive);

        if (contract == null)
        {
            throw new Exception("Nhân viên không có hợp đồng lao động hợp lệ.");
        }

        decimal tiLeLuong = (decimal)(contract.LoaiHopDong.TiLeLuong ?? 1.0);

        int standardWorkingDays = 26;
        int actualWorkingDays = attendanceRecords.Count;
        decimal totalHours = attendanceRecords.Sum(a => a.TongGio ?? 0);

        var holidays = await _context.NgayLes
            .Where(h => h.NgayLe1.Year == monthYear.Year &&
                        h.NgayLe1.Month == monthYear.Month &&
                        (h.TrangThai == null || h.TrangThai != "Đã hủy"))
            .ToListAsync();

        var approvedLeaves = await _context.NgayNghis
            .Include(n => n.MaLoaiNgayNghiNavigation)
            .Where(n => n.MaNv == employeeId &&
                        n.NgayNghi1.Year == monthYear.Year &&
                        n.NgayNghi1.Month == monthYear.Month &&
                        n.MaTrangThai == "NN2")
            .ToListAsync();

        var attendanceDates = attendanceRecords.Select(a => a.NgayLamViec).ToHashSet();

        int daysInMonth = DateTime.DaysInMonth(monthYear.Year, monthYear.Month);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateOnly(monthYear.Year, monthYear.Month, day);
            if (attendanceDates.Contains(currentDate))
                continue;

            bool isHoliday = holidays.Any(h =>
            {
                var holidayStart = h.NgayLe1;
                var holidayEnd = h.NgayLe1.AddDays((h.SoNgayNghi ?? 1) - 1);
                return currentDate >= holidayStart && currentDate <= holidayEnd;
            });

            bool isApprovedLeave = approvedLeaves.Any(l => l.NgayNghi1 == currentDate);

            if (isHoliday || isApprovedLeave)
            {
                totalHours += 8;
                actualWorkingDays++;
            }
        }

        decimal baseSalary = salaryInfo.LuongCoBan * ((decimal)actualWorkingDays / standardWorkingDays) * tiLeLuong;

        decimal hourlyRate = (salaryInfo.LuongCoBan * tiLeLuong) / 160;
        var overtimeRecords = await _context.TangCas
            .Where(t => t.MaNv == employeeId &&
                        t.NgayTangCa.Year == monthYear.Year &&
                        t.NgayTangCa.Month == monthYear.Month)
            .ToListAsync();
        decimal overtimeSalary = overtimeRecords.Sum(t => (decimal)t.SoGioTangCa * hourlyRate * t.TyLeTangCa);

        decimal grossSalary = baseSalary +
                             (salaryInfo.PhuCapCoDinh ?? 0) +
                             (salaryInfo.ThuongCoDinh ?? 0) +
                             overtimeSalary;

        decimal bhxh = salaryInfo.BHXH;
        decimal bhyt = salaryInfo.BHYT;
        decimal bhtn = salaryInfo.BHTN;

        decimal totalInsurance = bhxh + bhyt + bhtn;
        if (totalInsurance > grossSalary)
        {
            decimal ratio = grossSalary / totalInsurance;
            bhxh *= ratio;
            bhyt *= ratio;
            bhtn *= ratio;
            totalInsurance = grossSalary;
        }

        var giamTruGiaCanh = await _context.GiamTruGiaCanhs
            .Where(g => g.NgayHieuLuc <= monthYear && (g.NgayHetHieuLuc == null || g.NgayHetHieuLuc >= monthYear))
            .FirstOrDefaultAsync();

        if (giamTruGiaCanh == null)
        {
            throw new Exception("Không tìm thấy thông tin giảm trừ gia cảnh hợp lệ.");
        }

        var employee = await _context.NhanViens
            .FirstOrDefaultAsync(nv => nv.MaNv == employeeId);

        if (employee == null)
        {
            throw new Exception("Không tìm thấy thông tin nhân viên.");
        }

        decimal mucGiamTruBanThan = giamTruGiaCanh.MucGiamTruBanThan;
        decimal mucGiamTruNguoiPhuThuoc = giamTruGiaCanh.MucGiamTruNguoiPhuThuoc;
        int soNguoiPhuThuoc = employee.SoNguoiPhuThuoc;

        decimal giamTruTongCong = mucGiamTruBanThan + (soNguoiPhuThuoc * mucGiamTruNguoiPhuThuoc);
        decimal taxableIncome = grossSalary - bhxh - bhyt - bhtn - giamTruTongCong;

        decimal personalIncomeTax = 0;
        if (taxableIncome > 0)
        {
            personalIncomeTax = await CalculatePersonalIncomeTax(taxableIncome);
        }

        decimal netSalary = grossSalary - bhxh - bhyt - bhtn - personalIncomeTax;
        if (netSalary < 0)
        {
            netSalary = 0;
        }

        payroll.TongLuong = grossSalary;
        payroll.LuongTangCa = overtimeSalary;
        payroll.PhuCapThem = 0;
        payroll.LuongThem = 0;
        payroll.ThueTNCN = personalIncomeTax;
        payroll.ThucNhan = netSalary;

        return payroll;
    }

    private async Task<decimal> CalculatePersonalIncomeTax(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0;

        var taxBrackets = await _context.CauHinhLuongThues
            .OrderBy(t => t.MucLuongTu ?? 0)
            .ToListAsync();

        if (!taxBrackets.Any())
        {
            throw new Exception("Không tìm thấy cấu hình thuế TNCN trong bảng CauHinhLuongThue.");
        }

        decimal tax = 0;
        decimal remainingIncome = taxableIncome;

        foreach (var bracket in taxBrackets)
        {
            decimal from = bracket.MucLuongTu ?? 0;
            decimal to = bracket.MucLuongDen ?? decimal.MaxValue;
            decimal rate = bracket.GiaTri / 100;

            if (remainingIncome <= 0)
                break;

            decimal taxableInBracket = Math.Min(remainingIncome, to - from);
            if (taxableInBracket <= 0)
                continue;

            tax += taxableInBracket * rate;
            remainingIncome -= taxableInBracket;
        }

        return tax;
    }

    public async Task<(decimal BHXH, decimal BHYT, decimal BHTN, string ErrorMessage)> CalculateInsurance(decimal luongCoBan, decimal? phuCapCoDinh, decimal? thuongCoDinh)
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

            var luongDongBaoHiem = luongCoBan + (phuCapCoDinh ?? 0) + (thuongCoDinh ?? 0);

            var mucLuongToiThieuVung = activeMinimumWage.MucLuongToiThieuThang;
            var mucLuongToiDaBHXH_BHYT = activeBaseSalary.LuongCoSo * 20;
            var mucLuongToiDaBHTN = activeMinimumWage.MucLuongToiThieuThang * 20;

            if (luongDongBaoHiem < mucLuongToiThieuVung)
            {
                return (0, 0, 0, $"Mức lương đóng bảo hiểm (lương cơ bản + phụ cấp cố định + thưởng cố định) phải lớn hơn hoặc bằng mức lương tối thiểu vùng: {mucLuongToiThieuVung:N0} VNĐ.");
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