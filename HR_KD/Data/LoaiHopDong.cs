using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class LoaiHopDong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLoaiHopDong { get; set; }

        [Required]
        [StringLength(100)]
        public string TenLoaiHopDong { get; set; } = null!;

        [StringLength(500)]
        public string? MoTa { get; set; }

        public int? ThoiHanMacDinh { get; set; } // Thời hạn mặc định (tháng)

        [Required]
        public bool IsActive { get; set; } = true;

        [Range(0, 1)]
        public double? TiLeLuong { get; set; } // Ví dụ: 0.85 cho thử việc

        public int? GiaHanToiDa { get; set; } // Số lần gia hạn tối đa (null nếu không giới hạn)
    }
}
