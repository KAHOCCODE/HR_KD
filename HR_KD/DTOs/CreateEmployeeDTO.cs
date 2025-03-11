using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace HR_KD.DTOs
{
    public class CreateEmployeeDTO
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public bool? GioiTinh { get; set; }

        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(\+84|0)[1-9]\d{8,9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Sdt { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string? Email { get; set; } = null!;

        public string? TrinhDoHocVan { get; set; }

        [Required(ErrorMessage = "Phòng ban không được để trống")]
        public int MaPhongBan { get; set; }

        [Required(ErrorMessage = "Chức vụ không được để trống")]
        public int MaChucVu { get; set; }

        [AllowedExtensions(new string[] { ".jpg", ".png", ".jpeg" })]
        [MaxFileSize(5 * 1024 * 1024)] // Giới hạn 5MB
        public IFormFile? AvatarUrl { get; set; } // Ảnh đại diện
    }

    // Thuộc tính kiểm tra loại tệp tin (chỉ nhận .jpg, .png)
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = System.IO.Path.GetExtension(file.FileName);
                if (!_extensions.Contains(extension.ToLower()))
                {
                    return new ValidationResult($"Chỉ chấp nhận các định dạng: {string.Join(", ", _extensions)}");
                }
            }
            return ValidationResult.Success;
        }
    }

    // Thuộc tính kiểm tra kích thước tệp tin
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxSize;
        public MaxFileSizeAttribute(int maxSize)
        {
            _maxSize = maxSize;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxSize)
                {
                    return new ValidationResult($"Kích thước tệp không được vượt quá {_maxSize / (1024 * 1024)}MB");
                }
            }
            return ValidationResult.Success;
        }
    }
}
