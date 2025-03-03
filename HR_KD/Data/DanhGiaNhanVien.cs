using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class DanhGiaNhanVien
{
    public int MaDanhGia { get; set; }

    public int MaNv { get; set; }

    public int? MaNguoiDanhGia { get; set; }

    public DateOnly NgayDanhGia { get; set; }

    public decimal? DiemDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public virtual ICollection<ChiTietDanhGium> ChiTietDanhGia { get; set; } = new List<ChiTietDanhGium>();

    public virtual NhanVien? MaNguoiDanhGiaNavigation { get; set; }

    public virtual ICollection<TieuChiDanhGiaFullTime> TieuChiDanhGiaFullTimes { get; set; } = new List<TieuChiDanhGiaFullTime>();
}
