using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace HR_KD.DTOs
{
    public class CreateEmployeeDTO
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh không được để trống")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public bool GioiTinh { get; set; }

        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(\+84|0)[1-9]\d{8,9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Sdt { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string? TrinhDoHocVan { get; set; }

        [Required(ErrorMessage = "Phòng ban không được để trống")]
        public int MaPhongBan { get; set; }

        [Required(ErrorMessage = "Chức vụ không được để trống")]
        public int MaChucVu { get; set; }

        [Required(ErrorMessage = "Ngày vào làm không được để trống")]
        [DataType(DataType.Date)]
        public DateTime NgayVaoLam { get; set; }

        [AllowedExtensions(new string[] { ".jpg", ".png", ".jpeg" })]
        [MaxFileSize(5 * 1024 * 1024)]
        public IFormFile? AvatarUrl { get; set; }
        public int SoNguoiPhuThuoc { get; set; } = 0; // Thêm trường mới, mặc định là 0
    }

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
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult($"Chỉ chấp nhận các định dạng: {string.Join(", ", _extensions)}");
                }
            }
            return ValidationResult.Success;
        }
    }

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