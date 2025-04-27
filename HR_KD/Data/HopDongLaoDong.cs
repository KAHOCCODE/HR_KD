using HR_KD.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class HopDongLaoDong
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaHopDong { get; set; }

    [ForeignKey("NhanVien")]
    public int MaNv { get; set; }

    [ForeignKey("LoaiHopDong")]
    public int MaLoaiHopDong { get; set; }

    public int? ThoiHan { get; set; } // Thời hạn hợp đồng (tháng)

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? NgayKetThuc { get; set; } // Hạn kết thúc dự kiến (nếu có)

    [StringLength(1000)]
    public string? GhiChu { get; set; }

    public int? SoLanGiaHan { get; set; } // Số lần gia hạn hợp đồng

    [Required]
    public bool IsActive { get; set; } = true; // Còn hiệu lực hay không

    // === TÍCH HỢP NGHỈ VIỆC ===

    public DateOnly? NgayNghiViec { get; set; } // Ngày thực tế nghỉ việc

    [StringLength(255)]
    public string? LyDoNghiViec { get; set; } // Lý do nghỉ việc

    public virtual NhanVien NhanVien { get; set; } = null!;
    public virtual LoaiHopDong LoaiHopDong { get; set; } = null!;
}
