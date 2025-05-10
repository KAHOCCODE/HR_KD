using HR_KD.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HR_KD.Common;

public class PayrollCalculator
{
    private readonly HrDbContext _context;

    public PayrollCalculator(HrDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CalculateRemainingLeaveDays(int employeeId, int year)
    {
        // Lấy tất cả bản ghi PhepNamNhanVien của nhân viên từ năm hiện tại trở về trước, tối đa 3 năm
        var leaveRecords = await _context.PhepNamNhanViens
            .Where(s => s.MaNv == employeeId && s.Nam <= year)
            .OrderByDescending(s => s.Nam) // Sắp xếp giảm dần để lấy năm gần nhất trước
            .Take(3) // Chỉ lấy 3 năm gần nhất
            .ToListAsync();

        decimal remainingLeaveDays = 0;

        // Duyệt qua các bản ghi từ năm gần nhất đến xa nhất
        foreach (var record in leaveRecords)
        {
            // Lấy ngày chưa sử dụng từ năm hiện tại hoặc các năm có IsTinhLuong = false
            remainingLeaveDays += record.SoNgayChuaSuDung;

            // Nếu gặp năm có IsTinhLuong = true (không phải năm hiện tại), dừng lấy các năm trước đó
            if (record.IsTinhLuong && record.Nam != year)
            {
                break;
            }
        }

        return remainingLeaveDays;
    }

    public async Task<BangLuong> CalculatePayroll(int employeeId, DateTime monthYear, List<ChamCong> attendanceRecords, ThongTinLuongNV salaryInfo)
    {
        var payroll = new BangLuong
        {
            MaNv = employeeId,
            ThangNam = DateOnly.FromDateTime(monthYear),
            TrangThai = PayrollStatus.Created
        };

        var contract = await _context.HopDongLaoDongs
            .Include(hd => hd.LoaiHopDong)
            .FirstOrDefaultAsync(hd => hd.MaNv == employeeId && hd.IsActive);

        if (contract == null)
        {
            throw new Exception("Nhân viên không có hợp đồng lao động hợp lệ.");
        }

        decimal tiLeLuong = (decimal)(contract.LoaiHopDong.TiLeLuong ?? 1.0);

        // Get standard hours for the month from GioChuans
        var standardHoursRecord = await _context.GioChuans
            .FirstOrDefaultAsync(g => g.Nam == monthYear.Year && g.KichHoat == true);

        if (standardHoursRecord == null)
        {
            throw new Exception("Không tìm thấy thông tin giờ chuẩn cho tháng tính lương.");
        }

        decimal standardHours = monthYear.Month switch
        {
            1 => standardHoursRecord.Thang1,
            2 => standardHoursRecord.Thang2,
            3 => standardHoursRecord.Thang3,
            4 => standardHoursRecord.Thang4,
            5 => standardHoursRecord.Thang5,
            6 => standardHoursRecord.Thang6,
            7 => standardHoursRecord.Thang7,
            8 => standardHoursRecord.Thang8,
            9 => standardHoursRecord.Thang9,
            10 => standardHoursRecord.Thang10,
            11 => standardHoursRecord.Thang11,
            12 => standardHoursRecord.Thang12,
            _ => 0
        };

        // Calculate actual hours
        // Kiểm tra xem có bản ghi chấm công nào được duyệt (Approved hoặc Paidleave) không
        var validAttendanceRecords = attendanceRecords
            .Where(a => a.TrangThai == AttendanceStatus.Approved || a.TrangThai == AttendanceStatus.Paidleave)
            .ToList();

        if (!validAttendanceRecords.Any())
        {
            throw new Exception("Không có bản ghi chấm công nào được duyệt (Approved hoặc Paidleave). Vui lòng yêu cầu quản lý duyệt hoặc không duyệt trạng thái chấm công.");
        }

        decimal actualHours = validAttendanceRecords.Sum(a => a.TongGio ?? 0);

        // Lấy thông tin giờ thiếu và giờ bù từ bảng TongGioThieu
        var timeRecord = await _context.TongGioThieus
            .Where(t => t.MaNv == employeeId &&
                        t.NgayBatDauThieu.Year == monthYear.Year &&
                        t.NgayBatDauThieu.Month == monthYear.Month)
            .FirstOrDefaultAsync();

        decimal missingHours = timeRecord?.TongGioConThieu ?? 0;
        decimal makeupHours = timeRecord?.TongGioLamBu ?? 0;

        // Cập nhật actualHours: trừ giờ thiếu, cộng giờ bù
        actualHours = standardHours - missingHours + makeupHours;

        // Kiểm tra xem nhân viên có kích hoạt IsTinhLuong = true cho năm hiện tại không
        decimal remainingLeaveDays = 0;
        var currentYearLeave = await _context.PhepNamNhanViens
            .FirstOrDefaultAsync(p => p.MaNv == employeeId && p.Nam == monthYear.Year && p.IsTinhLuong == true);

        if (currentYearLeave != null)
        {
            // Nếu IsTinhLuong = true, tính số ngày phép còn lại
            remainingLeaveDays = await CalculateRemainingLeaveDays(employeeId, monthYear.Year);
        }

        // Lấy tổng giờ làm việc mỗi ngày từ ChamCongGioRaVaos
        var workingHoursRecord = await _context.ChamCongGioRaVaos
            .Where(c => c.KichHoat == true)
            .OrderByDescending(c => c.Id) // Lấy bản ghi mới nhất nếu có nhiều bản ghi KichHoat = true
            .FirstOrDefaultAsync();

        if (workingHoursRecord == null)
        {
            throw new Exception("Không tìm thấy cấu hình giờ làm việc trong ChamCongGioRaVaos.");
        }

        decimal dailyWorkingHours = workingHoursRecord.TongGio;

        // Cộng giờ từ ngày phép còn lại vào actualHours
        actualHours += remainingLeaveDays * dailyWorkingHours;

        decimal baseSalary = salaryInfo.LuongCoBan * (actualHours / standardHours) * tiLeLuong;

        // Calculate overtime
        decimal hourlyRate = (salaryInfo.LuongCoBan * tiLeLuong) / standardHours;
        var overtimeRecords = await _context.TangCas
            .Where(t => t.MaNv == employeeId &&
                        t.NgayTangCa.Year == monthYear.Year &&
                        t.NgayTangCa.Month == monthYear.Month &&
                        t.TrangThai == OvertimeStatus.Approved)
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
                return (0, 0, 0, "Không có mức lương tối thiểu vùng đang hoạt động.");
            }

            var activeBaseSalary = await _context.MucLuongCoSos
                .Where(m => m.IsActive)
                .FirstOrDefaultAsync();

            if (activeBaseSalary == null)
            {
                return (0, 0, 0, "Không có mức lương cơ sở đang hoạt động.");
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