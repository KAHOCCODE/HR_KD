using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class MucLuongCoSo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        public decimal LuongCoSo { get; set; } // Mức lương cơ sở (VD: 1.800.000)

        [Required]
        public DateTime NgayHieuLuc { get; set; } // Ngày bắt đầu áp dụng

        public DateTime? NgayHetHieuLuc { get; set; } // Ngày hết hiệu lực (nếu có)

        [StringLength(200)]
        public string GhiChu { get; set; } // Ghi chú (VD: "Theo Nghị định 73/2023/NĐ-CP")

        public bool IsActive { get; set; } = true;
    }
}