using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class LichSuChamCong
{
    public int MaLichSuChamCong { get; set; }

    public int MaNv { get; set; }

    public DateOnly Ngay { get; set; }

    public TimeOnly GioVao { get; set; }

    public TimeOnly? GioRa { get; set; }

    public decimal? TongGio { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
