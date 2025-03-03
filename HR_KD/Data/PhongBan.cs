using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class PhongBan
{
    public int MaPhongBan { get; set; }

    public string TenPhongBan { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
