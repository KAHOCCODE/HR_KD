using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class YeuCau
    {
        [Key]
        public int MaYeuCau { get; set; }
        public string TenYeuCau { get; set; } = null!;
        public string? MoTa { get; set; }
        public bool TrangThai { get; set; } = false;
        public string? MaNvTao { get; set; }
        public DateTime? NgayTao { get; set; }
        public string? MaNvDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }

    }
}
