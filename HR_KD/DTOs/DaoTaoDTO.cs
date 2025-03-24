using System.ComponentModel.DataAnnotations;

namespace HR_KD.DTOs
{
    public class DaoTaoDTO
    {
        public int MaDaoTao { get; set; }

        [Required(ErrorMessage = "Tên khóa đào tạo là bắt buộc")]
        public string TenDaoTao { get; set; }

        public string? MoTa { get; set; }

        public string? NoiDung { get; set; }

        public DateOnly? NgayBatDau { get; set; }

        public DateOnly? NgayKetThuc { get; set; }

        [Required(ErrorMessage = "Phòng ban là bắt buộc")]
        public int MaPhongBan { get; set; }

        public string? TenPhongBan { get; set; } // Để hiển thị tên phòng ban
    }
}