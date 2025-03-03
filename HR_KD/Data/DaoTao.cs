using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class DaoTao
{
    public int MaDaoTao { get; set; }

    public string TenDaoTao { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? NoiDung { get; set; }

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? NgayKetThuc { get; set; }

    public virtual ICollection<LichSuDaoTao> LichSuDaoTaos { get; set; } = new List<LichSuDaoTao>();
}
