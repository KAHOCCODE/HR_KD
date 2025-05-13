using HR_KD.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


public class PayrollReportService
{
    private readonly HrDbContext _context;

    public PayrollReportService(HrDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GeneratePayrollPdf(int maLuong)
    {
        var payroll = await _context.BangLuongs
            .Include(p => p.MaNvNavigation)
                .ThenInclude(nv => nv.MaPhongBanNavigation)
            .Include(p => p.MaNvNavigation.MaChucVuNavigation)
            .FirstOrDefaultAsync(p => p.MaLuong == maLuong);

        if (payroll == null) throw new Exception("Không tìm thấy bảng lương.");

        var nv = payroll.MaNvNavigation;
        var chucVu = nv.MaChucVuNavigation?.TenChucVu ?? "N/A";
        var phongBan = nv.MaPhongBanNavigation?.TenPhongBan ?? "N/A";

        var thongTinLuong = await _context.ThongTinLuongNVs
            .Where(x => x.MaNv == nv.MaNv)
            .OrderByDescending(x => x.NgayApDung)
            .FirstOrDefaultAsync();

        var hopDong = await _context.HopDongLaoDongs
            .Include(h => h.LoaiHopDong)
            .Where(h => h.MaNv == nv.MaNv && h.IsActive)
            .OrderByDescending(h => h.NgayBatDau)
            .FirstOrDefaultAsync();

        var taiKhoan = await _context.TaiKhoanNganHangs
            .Where(t => t.MaNv == nv.MaNv)
            .FirstOrDefaultAsync();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().AlignCenter().Text("BẢNG LƯƠNG NHÂN VIÊN")
                    .Bold().FontSize(16).Underline();

                page.Content().Column(col =>
                {
                    // A - Thông tin nhân viên
                    col.Item().PaddingBottom(5).Text($"Tháng: {payroll.ThangNam:MM/yyyy}").Bold();
                    col.Item().Text($"Họ tên: {nv.HoTen} | Mã NV: {nv.MaNv} | Giới tính: {(nv.GioiTinh == true ? "Nam" : "Nữ")}");
                    col.Item().Text($"Phòng ban: {phongBan} | Chức vụ: {chucVu}");
                    col.Item().Text($"Loại HĐ: {hopDong?.LoaiHopDong?.TenLoaiHopDong ?? "Không có"}");

                    col.Item().PaddingVertical(5).Text(" ");

                    // B - Thu nhập
                    col.Item().Text("B. THU NHẬP").Bold();
                    col.Item().Element(BuildIncomeTable(payroll, thongTinLuong));

                    // C - Khấu trừ
                    col.Item().PaddingTop(10).Text("C. KHẤU TRỪ").Bold();
                    col.Item().Element(BuildDeductionTable(payroll, thongTinLuong));

                    // D - Thực lãnh
                    col.Item().PaddingTop(10).Text($"D. LƯƠNG THỰC NHẬN: {payroll.ThucNhan:N0} VND").Bold().FontSize(12);

                    // E - Tài khoản ngân hàng
                    if (taiKhoan != null)
                    {
                        col.Item().PaddingTop(10).Text("E. THÔNG TIN TÀI KHOẢN").Bold();
                        col.Item().Text($"Ngân hàng: {taiKhoan.TenNganHang}");
                        col.Item().Text($"Chi nhánh: {taiKhoan.ChiNhanh}");
                        col.Item().Text($"Số tài khoản: {taiKhoan.SoTaiKhoan}");
                    }
                    else
                    {
                        col.Item().PaddingTop(10).Text("E. THÔNG TIN TÀI KHOẢN").Bold();
                        col.Item().Text("Không tìm thấy tài khoản ngân hàng.");
                    }

                    // F - Ghi chú
                    col.Item().PaddingTop(10).Text("F. GHI CHÚ").Bold();
                    col.Item().Text(payroll.GhiChu ?? "Không có");

                    // G - Chữ ký
                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Text("");
                        row.ConstantItem(250).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Người nhận lương").Bold();
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic();
                            c.Item().Height(60); // khoảng trắng cho chữ ký
                        });
                    });

                    col.Item().AlignRight().Text($"Ngày in: {DateTime.Now:dd/MM/yyyy}");
                });

                page.Footer().AlignCenter().Text("Bảng lương được tạo bởi hệ thống HR_KD").FontSize(8).Italic();
            });
        }).GeneratePdf();
    }

    private Action<IContainer> BuildIncomeTable(BangLuong bl, ThongTinLuongNV? tt)
    {
        return container => container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(30);
                c.RelativeColumn(3);
                c.RelativeColumn(2);
            });

            int stt = 1;
            void Add(string label, decimal value)
            {
                table.Cell().Element(CellStyle).Text((stt++).ToString());
                table.Cell().Element(CellStyle).Text(label);
                table.Cell().Element(CellStyle).Text($"{value:N0} VND");
            }

            Add("Lương cơ bản", tt?.LuongCoBan ?? 0);
            Add("Phụ cấp cố định", tt?.PhuCapCoDinh ?? 0);
            Add("Thưởng cố định", tt?.ThuongCoDinh ?? 0);
            Add("Phụ cấp thêm", bl.PhuCapThem);
            Add("Lương thêm", bl.LuongThem);
            Add("Tăng ca", bl.LuongTangCa);
            Add("Tổng lương", bl.TongLuong ?? 0);
        });
    }

    private Action<IContainer> BuildDeductionTable(BangLuong bl, ThongTinLuongNV? tt)
    {
        return container => container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(30);
                c.RelativeColumn(3);
                c.RelativeColumn(2);
            });

            int stt = 1;
            void Add(string label, decimal value)
            {
                table.Cell().Element(CellStyle).Text((stt++).ToString());
                table.Cell().Element(CellStyle).Text(label);
                table.Cell().Element(CellStyle).Text($"{value:N0} VND");
            }

            Add("BHXH", tt?.BHXH ?? 0);
            Add("BHYT", tt?.BHYT ?? 0);
            Add("BHTN", tt?.BHTN ?? 0);
            Add("Thuế TNCN", bl.ThueTNCN);
        });
    }

    private IContainer CellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5);
    }
}
