using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class BangLuong
{
    public int MaLuong { get; set; }

    public int MaNv { get; set; }

    public DateOnly ThangNam { get; set; }

    public decimal LuongCoBan { get; set; }

    public decimal? PhuCap { get; set; }

    public decimal? ThuNhapKhac { get; set; }

    public decimal? TongLuong { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
