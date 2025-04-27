using System;
using System.ComponentModel.DataAnnotations;

namespace HR_KD.DTOs
{
    public class ContractDTO
    {
        [Required(ErrorMessage = "Mã nhân viên không được để trống")]
        public int MaNv { get; set; }

        public int? MaHopDong { get; set; }

        [Required(ErrorMessage = "Loại hợp đồng không được để trống")]
        public int MaLoaiHopDong { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Thời hạn phải là số không âm")]
        public int? ThoiHan { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime NgayBatDau { get; set; }

        public DateTime? NgayKetThuc { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? GhiChu { get; set; }
    }
    public class ExtendContractDTO
    {
        public string MaHopDong { get; set; }
        public string MaNv { get; set; }
        public bool ConvertToUnlimited { get; set; }
    }
}