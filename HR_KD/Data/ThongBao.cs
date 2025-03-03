using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class ThongBao
{
    public int MaThongBao { get; set; }

    public int? MaNv { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDung { get; set; }

    public DateTime? NgayThongBao { get; set; }

    public bool? DaDoc { get; set; }

    public string? LoaiThongBao { get; set; }

    public virtual NhanVien? MaNvNavigation { get; set; }
}
