namespace HR_KD.DTOs
{
    public class ImportNhanVienDto
    {
        public string HoTen { get; set; } = "";
        public DateTime NgaySinh { get; set; }
        public bool GioiTinh { get; set; }
        public string Sdt { get; set; } = "";
        public string Email { get; set; } = "";
        public string TrinhDoHocVan { get; set; } = "";

        public string TenChucVu { get; set; } = "";
        public string TenPhongBan { get; set; } = "";

        public int? MaChucVu { get; set; }
        public int? MaPhongBan { get; set; }

        public DateTime NgayVaoLam { get; set; }
        public string? RowError { get; set; }
        public int RowIndex { get; set; }
    }
}
