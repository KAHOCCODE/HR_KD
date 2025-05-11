using System.ComponentModel.DataAnnotations;

namespace HR_KD.Data
{
    public class TrangThai
    {
        [Key]
        public string MaTrangThai { get; set; } = null!;

        public string TenTrangThai { get; set; } = null!;

        public string? MoTa { get; set; }

        // Các trạng thái ngày lễ
        public const string NL1 = "NL1"; // Ngày lễ thường
        public const string NL2 = "NL2"; // Ngày lễ rơi vào cuối tuần
        public const string NL3 = "NL3"; // Ngày nghỉ bù
        public const string NL4 = "NL4"; // Ngày lễ đã duyệt (có chấm công)
        public const string NL5 = "NL5"; // Ngày lễ cuối tuần đã duyệt (không chấm công)
        public const string NL6 = "NL6"; // Ngày lễ bị từ chối
    }
}
