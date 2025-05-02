using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class TrangThai
    {
        [Key]
        public string MaTrangThai { get; set; } = null!;

        public string TenTrangThai { get; set; } = null!;

        public string? MoTa { get; set; }
    }
}
