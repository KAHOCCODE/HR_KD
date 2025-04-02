using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class ThongTinLuongNV
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLuongNV { get; set; }

        [ForeignKey("NhanVien")]
        public int MaNv { get; set; }

        public decimal LuongCoBan { get; set; }

        public decimal? PhuCapCoDinh { get; set; } = 0;

        public decimal? ThuongCoDinh { get; set; } = 0;

        public DateTime NgayApDng { get; set; } = DateTime.Now;

        public string? GhiChu { get; set; }

        public virtual NhanVien NhanVien { get; set; } = null!;
    }
}
