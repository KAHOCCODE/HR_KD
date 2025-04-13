using HR_KD.Data;
using ClosedXML.Excel;
using HR_KD.DTOs;
using System.Text;
using OfficeOpenXml;
using System.ComponentModel;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace HR_KD.Services
{
    public class ExcelTemplateService
    {
        private readonly HrDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ExcelTemplateService(HrDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        #region Tạo file excel mẫu
        public byte[] GenerateTemplateWithData()
        {
            using var workbook = new XLWorkbook();

            // Sheet nhập dữ liệu
            var sheet = workbook.Worksheets.Add("NhanVienImport");

            var headers = new[]
            {
                "HoTen", "NgaySinh (dd/MM/yyyy)", "GioiTinh (Nam/Nu)", "SDT",
                "Email", "TrinhDoHocVan", "MaChucVu", "MaPhongBan", "NgayVaoLam (dd/MM/yyyy)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.SkyBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Cài format cột
            sheet.Column("B").Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Column("I").Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Column("D").Style.NumberFormat.Format = "@";

            // Auto-fit column width
            sheet.Columns().AdjustToContents();

            // Sheet tham chiếu
            var refSheet = workbook.Worksheets.Add("DanhMucThamChieu");
            refSheet.Cell("A1").Value = "MaChucVu";
            refSheet.Cell("B1").Value = "TenChucVu";
            refSheet.Cell("D1").Value = "MaPhongBan";
            refSheet.Cell("E1").Value = "TenPhongBan";

            var chucVus = _context.ChucVus.ToList();
            var phongBans = _context.PhongBans.ToList();

            for (int i = 0; i < chucVus.Count; i++)
            {
                refSheet.Cell(i + 2, 1).Value = chucVus[i].MaChucVu;
                refSheet.Cell(i + 2, 2).Value = chucVus[i].TenChucVu;
            }

            for (int i = 0; i < phongBans.Count; i++)
            {
                refSheet.Cell(i + 2, 4).Value = phongBans[i].MaPhongBan;
                refSheet.Cell(i + 2, 5).Value = phongBans[i].TenPhongBan;
            }

            // Tạo dropdown MaChucVu
            var chucVuRange = refSheet.Range($"A2:A{chucVus.Count + 1}");
            sheet.Range("G2:G100").CreateDataValidation().List(chucVuRange);

            // Tạo dropdown MaPhongBan
            var phongBanRange = refSheet.Range($"D2:D{phongBans.Count + 1}");
            sheet.Range("H2:H100").CreateDataValidation().List(phongBanRange);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        #endregion


        public async Task<ExcelImportResult> ParseExcelWithValidation(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var result = new ExcelImportResult();
            var employees = new List<ImportNhanVienDto>();
            var errors = new Dictionary<int, string>();

            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == "NhanVienImport");
            if (sheet == null)
            {
                result.Errors[0] = "Không tìm thấy sheet 'NhanVienImport'. Vui lòng dùng đúng file mẫu.";
                return result;
            }
            int row = 2;
            while (true)
            {
                var hoTen = sheet.Cells[row, 1]?.Text?.Trim();
                if (string.IsNullOrEmpty(hoTen)) break;

                var dto = new ImportNhanVienDto();
                var rowErrors = new StringBuilder();

                try
                {
                    dto.HoTen = hoTen;
                    dto.NgaySinh = DateTime.Parse(sheet.Cells[row, 2].Text.Trim());
                    dto.GioiTinh = sheet.Cells[row, 3].Text.Trim().ToLower() == "nam";
                    dto.Sdt = sheet.Cells[row, 4].Text.Trim();
                    dto.Email = sheet.Cells[row, 5].Text.Trim();
                    dto.TrinhDoHocVan = sheet.Cells[row, 6].Text.Trim();
                    dto.MaChucVu = int.TryParse(sheet.Cells[row, 7].Text.Trim(), out int macv) ? macv : (int?)null;
                    dto.MaPhongBan = int.TryParse(sheet.Cells[row, 8].Text.Trim(), out int mapb) ? mapb : (int?)null;
                    dto.NgayVaoLam = DateTime.Parse(sheet.Cells[row, 9].Text.Trim());

                    dto.TenChucVu = dto.MaChucVu != null
                        ? _context.ChucVus.FirstOrDefault(c => c.MaChucVu == dto.MaChucVu)?.TenChucVu
                        : null;

                    dto.TenPhongBan = dto.MaPhongBan != null
                        ? _context.PhongBans.FirstOrDefault(p => p.MaPhongBan == dto.MaPhongBan)?.TenPhongBan
                        : null;

                    if (_context.TaiKhoans.Any(t => t.Username == dto.Sdt))
                        rowErrors.Append("SĐT đã tồn tại. ");

                    if (_context.NhanViens.Any(e => e.Email == dto.Email))
                        rowErrors.Append("Email đã tồn tại. ");

                    if (!_context.ChucVus.Any(c => c.TenChucVu == dto.TenChucVu))
                        rowErrors.Append("Chức vụ không tồn tại. ");

                    if (!_context.PhongBans.Any(p => p.TenPhongBan == dto.TenPhongBan))
                        rowErrors.Append("Phòng ban không tồn tại. ");
                }
                catch (Exception ex)
                {
                    rowErrors.Append("Lỗi định dạng: " + ex.Message);
                }

                employees.Add(dto);
                if (rowErrors.Length > 0)
                    errors[row] = rowErrors.ToString();

                row++;
            }

            result.Employees = employees;
            result.Errors = errors;
            return result;
        }

    }
}
