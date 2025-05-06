using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class TangCa
{
    public int MaTangCa { get; set; }

    public int MaNv { get; set; }

    public DateOnly NgayTangCa { get; set; }

    public decimal SoGioTangCa { get; set; }

    public decimal TyLeTangCa { get; set; }

    public string? TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public int MaNvDuyet { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
    public TimeOnly? GioVao { get; set; }
    public TimeOnly? GioRa { get; set; }
}
