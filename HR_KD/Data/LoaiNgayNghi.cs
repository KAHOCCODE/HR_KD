using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class LoaiNgayNghi
{
    public int MaLoaiNgayNghi { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool HuongLuong { get; set; }
    public bool TinhVaoPhepNam { get; set; }
    public bool CoTinhVaoLuong { get; set; }

    public virtual ICollection<NgayNghi> NgayNghis { get; set; } = new List<NgayNghi>();
}
