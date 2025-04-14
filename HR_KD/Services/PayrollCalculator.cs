using HR_KD.Data;
using System;
using System.Collections.Generic;
using System.Linq;

public class PayrollCalculator
{
    private readonly HrDbContext _context;

    public PayrollCalculator(HrDbContext context)
    {
        _context = context;
    }

    public BangLuong CalculatePayroll(int employeeId, DateTime monthYear, List<ChamCong> attendanceRecords, ThongTinLuongNV salaryInfo)
    {
        var payroll = new BangLuong
        {
            MaNv = employeeId,
            ThangNam = DateOnly.FromDateTime(monthYear),
            TrangThai = "Đã tạo - Chưa thanh toán"
        };

        // Tính tổng giờ làm  
        decimal totalHours = attendanceRecords.Sum(a => a.TongGio ?? 0);

        // Tính lương cơ bản theo giờ (giả định 160 giờ/tháng)  
        decimal hourlyRate = salaryInfo.LuongCoBan / 160;
        decimal baseSalary = totalHours * hourlyRate;

        // Tính lương tăng ca (giả định 1.5x)  
        var overtimeRecords = _context.TangCas
            .Where(t => t.MaNv == employeeId &&
                        t.NgayTangCa.Year == monthYear.Year &&
                        t.NgayTangCa.Month == monthYear.Month)
            .ToList();
        decimal overtimeHours = overtimeRecords.Sum(t => (decimal?)t.SoGioTangCa ?? 0);
        decimal overtimeSalary = overtimeHours * hourlyRate * 1.5m;

        // Tính tổng lương trước khấu trừ  
        decimal grossSalary = baseSalary +
                             (salaryInfo.PhuCapCoDinh ?? 0) +
                             (salaryInfo.ThuongCoDinh ?? 0) +
                             overtimeSalary;

        // Tính các khoản khấu trừ  
        decimal bhyt = salaryInfo.BHYT;
        decimal bhxh = salaryInfo.BHXH;
        decimal bhtn = salaryInfo.BHTN;

        // Tính thuế TNCN  
        decimal taxableIncome = grossSalary - bhyt - bhxh - bhtn;
        decimal personalIncomeTax = CalculatePersonalIncomeTax(taxableIncome);

        // Tính lương thực nhận  
        decimal netSalary = grossSalary - bhyt - bhxh - bhtn - personalIncomeTax;

        // Gán giá trị cho bảng lương  
        payroll.TongLuong = grossSalary;
        payroll.LuongTangCa = overtimeSalary;
        payroll.PhuCapThem = 0; // Gán giá trị 0
        payroll.LuongThem = 0;  // Gán giá trị 0
        payroll.ThueTNCN = personalIncomeTax;
        payroll.ThucNhan = netSalary;

        return payroll;
    }

    private decimal CalculatePersonalIncomeTax(decimal taxableIncome)
    {
        var taxConfig = _context.CauHinhLuongThues
            .Where(c => c.MucLuongTu <= taxableIncome && taxableIncome <= c.MucLuongDen)
            .OrderBy(c => c.MucLuongTu)
            .FirstOrDefault();

        return taxConfig != null ? taxableIncome * (taxConfig.GiaTri / 100) : 0;
    }
}