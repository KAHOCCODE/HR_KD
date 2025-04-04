﻿using System;
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

    // Remove BHXH, BHYT, and BHTN fields
    // public decimal BHXH { get; set; } = 0;
    // public decimal BHYT { get; set; } = 0;
    // public decimal BHTN { get; set; } = 0;

    public decimal ThueTNCN { get; set; } = 0;

    // Remove the computed column for TongLuong
    public decimal? TongLuong { get; set; }

    public decimal ThucNhan { get; set; }

    public string? TrangThai { get; set; } = "Chưa thanh toán";

    public string? GhiChu { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}