using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class LichSuDaoTao
{
    public int MaLichSu { get; set; }

    public int MaNv { get; set; }

    public int MaDaoTao { get; set; }

    public string? KetQua { get; set; }

    public virtual DaoTao MaDaoTaoNavigation { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
