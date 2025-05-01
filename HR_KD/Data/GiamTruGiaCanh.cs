using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class GiamTruGiaCanh
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MucGiamTruBanThan { get; set; } // Mức giảm trừ cho bản thân (VD: 11,000,000 VNĐ/tháng)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MucGiamTruNguoiPhuThuoc { get; set; } // Mức giảm trừ cho mỗi người phụ thuộc (VD: 4,400,000 VNĐ/tháng)

        [Required]
        public DateTime NgayHieuLuc { get; set; } // Ngày bắt đầu áp dụng mức giảm trừ

        public DateTime? NgayHetHieuLuc { get; set; } // Ngày hết hiệu lực (nếu có)

        [StringLength(200)]
        public string GhiChu { get; set; } // Ghi chú thêm (VD: "Theo Luật thuế TNCN 2023")
    }
}