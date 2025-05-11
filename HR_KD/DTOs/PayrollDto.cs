namespace HR_KD.DTOs
{
    internal class PayrollDto
    {
        public int MaLuong { get; set; }
        public int MaNv { get; set; }
        public DateOnly ThangNam { get; set; }
        public decimal PhuCapThem { get; set; }
        public decimal LuongThem { get; set; }
        public decimal LuongTangCa { get; set; }
        public decimal ThueTNCN { get; set; }
        public decimal? TongLuong { get; set; }
        public decimal ThucNhan { get; set; }
        public string NguoiTao { get; set; }
        public DateTime NgayTao { get; set; }
        public string TrangThai { get; set; }
        public string? TenTrangThai { get; set; }
        public string GhiChu { get; set; }
    }
}