using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_KD.Data;

public partial class NgayNghi
{
    public int MaNgayNghi { get; set; }

    public int MaNv { get; set; }

    public DateOnly NgayNghi1 { get; set; }

    public string? LyDo { get; set; }
    
    public int? MaLoaiNgayNghi { get; set; }

    public DateTime NgayLamDon { get; set; }

    public int? NguoiDuyetId { get; set; }

    public DateTime NgayDuyet { get; set; } = DateTime.Now;

    public string MaTrangThai { get; set; } = "NN1";


    public string? GhiChu { get; set; }
    public string? LyDoTuChoi { get; set; }

    public string? LyDoHuy { get; set; }


    public string? FileDinhKem { get; set; }
    public string MaDon { get; set; } = null!; // Thêm trường MaDon để nhóm đơn đăng ký

    public virtual LoaiNgayNghi? MaLoaiNgayNghiNavigation { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;

    public virtual NhanVien? NguoiDuyet { get; set; }

}
