using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class ThongTinBaoHiem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string LoaiBaoHiem { get; set; } // BHYT, BHTN, BHXH

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TyLeNguoiLaoDong { get; set; } // Tỷ lệ đóng của người lao động (%)

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal TyLeNhaTuyenDung { get; set; } // Tỷ lệ đóng của nhà tuyển dụng (%)

        public DateTime NgayHieuLuc { get; set; } // Ngày bắt đầu áp dụng tỷ lệ

        public DateTime? NgayHetHieuLuc { get; set; } // Ngày hết hiệu lực (nếu có)

        [StringLength(200)]
        public string GhiChu { get; set; } // Ghi chú thêm
    }
}