using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class NhanVien
{
    public int MaNv { get; set; }

    public string HoTen { get; set; } = null!;

    public DateOnly? NgaySinh { get; set; }

    public bool? GioiTinh { get; set; }

    public string? DiaChi { get; set; }

    public string? Sdt { get; set; }

    public string? Email { get; set; }

    public string? TrinhDoHocVan { get; set; }

    public int MaPhongBan { get; set; }

    public int MaChucVu { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual ICollection<BangLuong> BangLuongs { get; set; } = new List<BangLuong>();

    public virtual ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();

    public virtual ICollection<DanhGiaNhanVien> DanhGiaNhanViens { get; set; } = new List<DanhGiaNhanVien>();

    public virtual ICollection<LichSuChamCong> LichSuChamCongs { get; set; } = new List<LichSuChamCong>();

    public virtual ICollection<LichSuDaoTao> LichSuDaoTaos { get; set; } = new List<LichSuDaoTao>();

    public virtual ChucVu MaChucVuNavigation { get; set; } = null!;

    public virtual PhongBan MaPhongBanNavigation { get; set; } = null!;

    public virtual ICollection<NgayNghi> NgayNghis { get; set; } = new List<NgayNghi>();

    public virtual ICollection<SoDuPhep> SoDuPheps { get; set; } = new List<SoDuPhep>();

    public virtual ICollection<TaiKhoanNganHang> TaiKhoanNganHangs { get; set; } = new List<TaiKhoanNganHang>();

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();

    public virtual ICollection<TangCa> TangCas { get; set; } = new List<TangCa>();

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<YeuCauSuaChamCong> YeuCauSuaChamCongs { get; set; } = new List<YeuCauSuaChamCong>();
}
