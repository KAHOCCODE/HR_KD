using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
namespace HR_KD.DTOs
{
    public class ChamCongDTO
    {
        public string NgayLamViec { get; set; }
        public string? GioVao { get; set; }
        public string? GioRa { get; set; }
        public decimal? TongGio { get; set; }
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }
    }
}
