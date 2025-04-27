using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class VungLuongTheoDiaPhuong
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TinhThanh { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string QuanHuyen { get; set; } = null!;

        [Required]
        public int VungLuong { get; set; }  // 1, 2, 3, 4

        [MaxLength(255)]
        public string? GhiChu { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("VungLuong")]
        public MucLuongToiThieuVung MucLuongVung { get; set; } // Navigation property
    }
}
