using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HR_KD.Data
{
    

        public class LamBu
        {
            [Key]
            public int MaLamBu { get; set; }

            [Required]
            public int MaNV { get; set; }

            [Required]
            public DateOnly NgayLamViec { get; set; }

            public TimeOnly? GioVao { get; set; }
            public TimeOnly? GioRa { get; set; }

            [Column(TypeName = "decimal(5, 2)")]
            public decimal? TongGio { get; set; }

            [MaxLength(50)]
            public string? TrangThai { get; set; }

            [MaxLength(255)]
            public string? GhiChu { get; set; }

            // Optional: navigation property
            public NhanVien? NhanVien { get; set; }
            public int MaNvDuyet { get; set; }
    }

 }

