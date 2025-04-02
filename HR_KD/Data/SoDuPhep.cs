using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace HR_KD.Data;

public partial class SoDuPhep
{
    public int MaNv { get; set; }

    public int Nam { get; set; }

    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    public decimal SoNgayConLai { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
