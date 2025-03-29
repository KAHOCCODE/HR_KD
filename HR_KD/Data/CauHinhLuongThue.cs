using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class CauHinhLuongThue
{
    public int MaCauHinh { get; set; }

    public decimal? MucLuongTu { get; set; }

    public decimal? MucLuongDen { get; set; }

    public decimal GiaTri { get; set; }

    public string? MoTa { get; set; }
}
