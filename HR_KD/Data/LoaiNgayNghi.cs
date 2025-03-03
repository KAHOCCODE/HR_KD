using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class LoaiNgayNghi
{
    public int MaLoaiNgayNghi { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<NgayNghi> NgayNghis { get; set; } = new List<NgayNghi>();

    public virtual ICollection<SoDuPhep> SoDuPheps { get; set; } = new List<SoDuPhep>();
}
