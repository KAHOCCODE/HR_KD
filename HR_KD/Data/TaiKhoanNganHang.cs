using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class TaiKhoanNganHang
{
    public int MaTk { get; set; }

    public int MaNv { get; set; }

    public string SoTaiKhoan { get; set; } = null!;

    public string? TenNganHang { get; set; }

    public string? ChiNhanh { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
