using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
namespace HR_KD.DTOs
{
    public class ApproveAttendanceRequestDTO
    {
        public int MaChamCong { get; set; }       // Chính là MaLichSuChamCong
        public string TrangThai { get; set; } = ""; // "Đã duyệt" hoặc "Từ chối"
        public string? GhiChu { get; set; } = ""; // Ghi chú cho việc duyệt hoặc từ chối

    }
}
