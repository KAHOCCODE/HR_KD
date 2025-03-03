using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class TaiKhoan
{
    public string Username { get; set; } = null!;

    public int MaNv { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string MaQuyenHan { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    public virtual QuyenHan MaQuyenHanNavigation { get; set; } = null!;
}
