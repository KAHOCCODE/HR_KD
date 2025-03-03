using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class ChucVu
{
    public int MaChucVu { get; set; }

    public string TenChucVu { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
