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

        [Required]
        public int VungLuongTheoDiaPhuongId { get; set; } // Tham chiếu đến địa phương

        [ForeignKey("VungLuongTheoDiaPhuongId")]
        public VungLuongTheoDiaPhuong VungLuongTheoDiaPhuong { get; set; } // Navigation property

        [Required]
        public int MucLuongCoSoId { get; set; } // Tham chiếu đến mức lương cơ sở

        [ForeignKey("MucLuongCoSoId")]
        public MucLuongCoSo MucLuongCoSo { get; set; } // Navigation property

        public DateTime NgayHieuLuc { get; set; } // Ngày bắt đầu áp dụng tỷ lệ

        public DateTime? NgayHetHieuLuc { get; set; } // Ngày hết hiệu lực (nếu có)

        [StringLength(200)]
        public string GhiChu { get; set; } // Ghi chú thêm

        // Mức lương tối đa cho BHXH/BHYT (20 lần mức lương cơ sở)
        [NotMapped]
        public decimal MucLuongToiDaBHXH_BHYT => MucLuongCoSo?.LuongCoSo * 20 ?? 0;

        // Mức lương tối đa cho BHTN (20 lần mức lương tối thiểu vùng)
        [NotMapped]
        public decimal MucLuongToiDaBHTN => VungLuongTheoDiaPhuong?.MucLuongVung?.MucLuongToiThieuThang * 20 ?? 0;

        // Mức lương tối thiểu (lấy từ vùng lương)
        [NotMapped]
        public decimal MucLuongToiThieu => VungLuongTheoDiaPhuong?.MucLuongVung?.MucLuongToiThieuThang ?? 0;
    }
}