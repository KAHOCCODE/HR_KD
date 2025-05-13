using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class NgayLeCoDinh
    {
        [Key]   
        
        public int MaNgayLe { get; set; }

        public string TenNgayLe { get; set; } = null!;

        public DateOnly NgayLe1 { get; set; }

        public int? SoNgayNghi { get; set; }

        public string? MoTa { get; set; }
    }

}

