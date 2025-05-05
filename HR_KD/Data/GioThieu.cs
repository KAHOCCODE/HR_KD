using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class GioThieu
    {
        [Key]
        public int MaGioThieu { get; set; }
        public DateOnly NgayThieu { get; set; }
        public decimal TongGioThieu { get; set; }
        public int MaNv { get; set; }
        // liên kết mã nhân viên
        public virtual NhanVien MaNvNavigation { get; set; } = null!;
    }
}
