namespace HR_KD.DTOs
{
    public class ThongTinLuongNVDTO
    {
        public int MaLuongNV { get; set; }
        public int MaNv { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal? PhuCapCoDinh { get; set; }
        public decimal? ThuongCoDinh { get; set; }
        public decimal BHXH { get; set; }
        public decimal BHYT { get; set; }
        public decimal BHTN { get; set; }
        public DateTime NgayApDng { get; set; }
        public string? GhiChu { get; set; }
    }
}