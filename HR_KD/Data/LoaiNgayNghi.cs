using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace HR_KD.Data;


public class LoaiNgayNghiMetadata
{
    [DisplayName("Mã loại")]
    public int MaLoaiNgayNghi { get; set; }

    [Required(ErrorMessage = "Tên loại ngày nghỉ không được để trống")]
    [StringLength(100, ErrorMessage = "Tên loại ngày nghỉ không được vượt quá 100 ký tự")]
    [DisplayName("Tên loại")]
    public string TenLoai { get; set; } = null!;

    [DisplayName("Mô tả")]
    public string? MoTa { get; set; }

    [DisplayName("Hưởng lương")]
    public bool HuongLuong { get; set; }

    [DisplayName("Tính vào phép năm")]
    public bool TinhVaoPhepNam { get; set; }

    [DisplayName("Tính vào lương")]
    public bool CoTinhVaoLuong { get; set; }
}

// Áp dụng metadata cho class LoaiNgayNghi
[MetadataType(typeof(LoaiNgayNghiMetadata))]

public partial class LoaiNgayNghi
{
    public int MaLoaiNgayNghi { get; set; }

    public string TenLoai { get; set; } = null!;

    public string? MoTa { get; set; }

    public bool HuongLuong { get; set; }
    public bool TinhVaoPhepNam { get; set; }
    public bool CoTinhVaoLuong { get; set; }

    public virtual ICollection<NgayNghi> NgayNghis { get; set; } = new List<NgayNghi>();
}
