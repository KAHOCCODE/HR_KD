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
    public int? SoNgayNghiToiDa { get; set; }
    public int? SoLanDangKyToiDa { get; set; }


    [DisplayName("Hưởng lương")]
    public bool HuongLuong { get; set; }

    [DisplayName("Tính vào phép năm")]
    public bool TinhVaoPhepNam { get; set; }


}

// Áp dụng metadata cho class LoaiNgayNghi
[MetadataType(typeof(LoaiNgayNghiMetadata))]

public partial class LoaiNgayNghi
{
    [DisplayName("Mã loại")]
    public int MaLoaiNgayNghi { get; set; }

    [Required(ErrorMessage = "Tên loại ngày nghỉ không được để trống")]
    [StringLength(100, ErrorMessage = "Tên loại ngày nghỉ không được vượt quá 100 ký tự")]
    [DisplayName("Tên loại")]
    public string TenLoai { get; set; } = null!;

    [DisplayName("Mô tả")]
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]

    public string? MoTa { get; set; }
    [DisplayName("Số ngày nghỉ tối đa (bỏ trống nếu không giới hạn)")]
    [Range(0, 365, ErrorMessage = "Phải từ 0 đến 365 ngày")]
    public int? SoNgayNghiToiDa { get; set; }

    [DisplayName("Số lần đăng ký tối đa (bỏ trống nếu không giới hạn)")]
    [Range(1, 100, ErrorMessage = "Phải từ 1 đến 100 lần")]
    public int? SoLanDangKyToiDa { get; set; }


    public bool HuongLuong { get; set; }
    public bool TinhVaoPhepNam { get; set; }


    public virtual ICollection<NgayNghi> NgayNghis { get; set; } = new List<NgayNghi>();
}
