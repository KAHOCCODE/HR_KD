using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class NgayLe
{
    public int MaNgayLe { get; set; }

    public string TenNgayLe { get; set; } = null!;

    public DateOnly NgayLe1 { get; set; }

    public int? SoNgayNghi { get; set; }

    public string? TrangThai { get; set; }

    public string? MoTa { get; set; }
}
