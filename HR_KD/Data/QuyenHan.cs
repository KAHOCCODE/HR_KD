using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class QuyenHan
{
    public string MaQuyenHan { get; set; } = null!;

    public string TenQuyenHan { get; set; } = null!;

    public string MoTaQuyenHan { get; set; } = null!;

    public virtual ICollection<TaiKhoanQuyenHan> TaiKhoanQuyenHans { get; set; } = new List<TaiKhoanQuyenHan>();

}
