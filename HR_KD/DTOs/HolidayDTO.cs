using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
namespace HR_KD.DTOs
{
    public class HolidayDTO
    {
        public int MaNgayLe { get; set; }
        public string TenNgayLe { get; set; } = null!;
        public DateTime NgayLe1 { get; set; }
        public int? SoNgayNghi { get; set; }
        public string? MoTa { get; set; }
    }
}
