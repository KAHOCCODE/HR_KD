using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class TieuChiDanhGiaFullTime
{
    public int MaTieuChiDanhGia { get; set; }

    public int MaDanhGia { get; set; }

    public string TenDanhGia { get; set; } = null!;

    public string MoTaDanhGia { get; set; } = null!;

    public decimal? DiemDanhGia { get; set; }

    public virtual ICollection<ChiTietDanhGium> ChiTietDanhGia { get; set; } = new List<ChiTietDanhGium>();

    public virtual DanhGiaNhanVien MaDanhGiaNavigation { get; set; } = null!;
}
