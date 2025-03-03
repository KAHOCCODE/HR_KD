using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class ChamCong
{
    public int MaChamCong { get; set; }

    public int MaNv { get; set; }

    public DateOnly NgayLamViec { get; set; }

    public TimeOnly? GioVao { get; set; }

    public TimeOnly? GioRa { get; set; }

    public decimal? TongGio { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
