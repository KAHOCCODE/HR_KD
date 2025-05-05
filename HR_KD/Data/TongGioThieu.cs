using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class TongGioThieu
    {
        [Key]
        public int MaTongGioThieu { get; set; }
        public int MaNv { get; set; }
        public DateOnly NgayBatDauThieu { get; set; }
        public DateOnly NgayKetThucThieu { get; set; }
        public decimal TongGioConThieu { get; set; }
        public decimal TongGioLamBu { get; set; }
        
        // liên kết mã nhân viên
        public virtual NhanVien MaNvNavigation { get; set; } = null!;

    }
}
