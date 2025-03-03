using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class YeuCauSuaChamCong
{
    public int MaYeuCau { get; set; }

    public int MaNv { get; set; }

    public DateOnly NgayLamViec { get; set; }

    public TimeOnly? GioVaoMoi { get; set; }

    public TimeOnly? GioRaMoi { get; set; }

    public int? TongGio { get; set; }

    public string? LyDo { get; set; }

    public int TrangThai { get; set; }

    public DateTime? NgayYeuCau { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
