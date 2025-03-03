using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class TieuChiDanhGiaPartTime
{
    public int MaTieuChiDanhGia { get; set; }

    public string TenDanhGia { get; set; } = null!;

    public string MoTaDanhGia { get; set; } = null!;

    public decimal DiemDanhGia { get; set; }
}
