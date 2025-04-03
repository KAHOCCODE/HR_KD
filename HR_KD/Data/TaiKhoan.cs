using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class TaiKhoan
{
    public string Username { get; set; } = null!;

    public int MaNv { get; set; }

    public string PasswordHash { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    public virtual ICollection<TaiKhoanQuyenHan> TaiKhoanQuyenHans { get; set; } = new List<TaiKhoanQuyenHan>();
}
