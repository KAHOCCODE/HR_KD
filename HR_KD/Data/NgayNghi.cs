using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class NgayNghi
{
    public int MaNgayNghi { get; set; }

    public int MaNv { get; set; }

    public DateOnly NgayNghi1 { get; set; }

    public string? LyDo { get; set; }

    public string? TrangThai { get; set; }

    public int? MaLoaiNgayNghi { get; set; }

    public DateTime NgayNgayLamDon { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime NgayDuyet { get; set; } = DateTime.Now;

    public string? GhiChu { get; set; }

    public string? FileDinhKem { get; set; }

    public virtual LoaiNgayNghi? MaLoaiNgayNghiNavigation { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    public virtual NhanVien? NguoiDuyet { get; set; }
}
