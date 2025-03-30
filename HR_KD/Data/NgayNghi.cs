using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class NgayNghi
{
    public int MaNgayNghi { get; set; }

    public int MaNv { get; set; }

    public DateOnly NgayNghi1 { get; set; }

    public string? LyDo { get; set; }

    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    public string? TrangThai { get; set; }

    public int? MaLoaiNgayNghi { get; set; }

    public virtual LoaiNgayNghi? MaLoaiNgayNghiNavigation { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
