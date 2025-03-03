using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class SoDuPhep
{
    public int MaNv { get; set; }

    public int MaLoaiNgayNghi { get; set; }

    public int Nam { get; set; }

    public decimal SoNgayConLai { get; set; }

    public virtual LoaiNgayNghi MaLoaiNgayNghiNavigation { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
