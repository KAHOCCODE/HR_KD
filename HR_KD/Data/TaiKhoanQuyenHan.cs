using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data
{
    public class TaiKhoanQuyenHan
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int Id { get; set; }

        public string Username { get; set; } = null!;

        public string MaQuyenHan { get; set; } = null!;

        [ForeignKey("Username")]
        public virtual TaiKhoan TaiKhoan { get; set; } = null!;

        [ForeignKey("MaQuyenHan")]
        public virtual QuyenHan QuyenHan { get; set; } = null!;
    }
}
