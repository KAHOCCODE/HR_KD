using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class MucLuongToiThieuVung
    {
        [Key]
        public int VungLuong { get; set; } // 1, 2, 3, 4 tương ứng Vùng I, II, III, IV

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        public decimal MucLuongToiThieuThang { get; set; } // Lưu dưới dạng số nguyên (VD: 4.960.000)

        [Required]
        [Column(TypeName = "decimal(18,0)")]
        public decimal MucLuongToiThieuGio { get; set; } // Lưu dưới dạng số nguyên (VD: 23.800)
    }
}