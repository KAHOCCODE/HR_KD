using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class ChiTietDanhGium
{
    public int MaChiTietDanhGia { get; set; }

    public int MaDanhGia { get; set; }

    public decimal? DiemDanhGia { get; set; }

    public int? MaTieuChiDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public virtual DanhGiaNhanVien MaDanhGiaNavigation { get; set; } = null!;

    public virtual TieuChiDanhGiaFullTime? MaTieuChiDanhGiaNavigation { get; set; }
}
