using System;
using System.Collections.Generic;

namespace HR_KD.Data;

public partial class BangLuong
{
    public int MaLuong { get; set; }

    public int MaNv { get; set; }

    public DateOnly ThangNam { get; set; }

    public decimal PhuCapThem { get; set; } = 0;

    public decimal LuongThem { get; set; } = 0;

    public decimal LuongTangCa { get; set; } = 0;

    public decimal ThueTNCN { get; set; } = 0;

    public decimal? TongLuong { get; set; }

    public decimal ThucNhan { get; set; }

    public string? NguoiTao { get; set; }

    public DateTime NgayTao { get; set; }

    public string? TrangThai { get; set; } 

    public string? GhiChu { get; set; }

    public string? NguoiDuyetNV { get; set; } // Người duyệt/xác nhận bởi Nhân viên
    public DateTime? NgayDuyetNV { get; set; } // Ngày duyệt/xác nhận bởi Nhân viên

    public string? NguoiTuChoiNV { get; set; } // Người từ chối bởi Nhân viên
    public DateTime? NgayTuChoiNV { get; set; } // Ngày từ chối bởi Nhân viên

    public string? NguoiGuiKT { get; set; } // Người gửi Kế toán (Thường là Trưởng phòng)
    public DateTime? NgayGuiKT { get; set; } 

    public string? NguoiTraVeKT { get; set; } // Người bị Kế toán trả về (Thường là Trưởng phòng) 
    public string? NguoiTraVeTuKT { get; set; } // Người trả về từ Kế toán (Kế toán) 
    public DateTime? NgayTraVeTuKT { get; set; } // Ngày trả về từ Kế toán

    public string? NguoiDuyetKT { get; set; } // Người duyệt bởi Kế toán
    public DateTime? NgayDuyetKT { get; set; } // Ngày duyệt bởi Kế toán

    public string? NguoiTraVeGD { get; set; } 
    public string? NguoiTraVeTuGD { get; set; } 
    public DateTime? NgayTraVeTuGD { get; set; } 

    public string? NguoiDuyetGD { get; set; } 
    public DateTime? NgayDuyetGD { get; set; } 
    public string? NguoiGuiNV { get; set; } 
    public DateTime? NgayGuiNV { get; set; } 

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}