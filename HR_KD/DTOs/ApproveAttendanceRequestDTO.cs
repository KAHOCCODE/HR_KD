using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
namespace HR_KD.DTOs
{
    public class ApproveAttendanceRequestDTO
    {
        public int MaChamCong { get; set; }
        public string TrangThai { get; set; } = "Chờ duyệt";
    }
}
