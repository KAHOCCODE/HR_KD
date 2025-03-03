using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class CauHinhLuongThue
{
    public int MaCauHinh { get; set; }

    public string TenCauHinh { get; set; } = null!;

    public decimal GiaTri { get; set; }

    public string? MoTa { get; set; }
}
