using HR_KD.Data;
using System.ComponentModel.DataAnnotations;

public class PhepNamNhanVien
{
    [Key]
    public int MaNv { get; set; }

    [Key]
    public int Nam { get; set; }

    [Required]
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    [Required]
    public decimal SoNgayPhepDuocCap { get; set; }

    [Required]
    public decimal SoNgayDaSuDung { get; set; }

    [StringLength(500)]
    public string? GhiChu { get; set; }

    [Required]
    public int CauHinhPhepNamId { get; set; }

    public bool IsReset { get; set; } = false; 

    public virtual CauHinhPhepNam CauHinhPhepNam { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}