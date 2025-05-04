using System.ComponentModel.DataAnnotations;

namespace HR_KD.Models
{
    public class ChinhSachPhepNamViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chính sách không được để trống")]
        [Display(Name = "Tên chính sách")]
        public string TenChinhSach { get; set; }

        [Required(ErrorMessage = "Số năm không được để trống")]
        [Display(Name = "Số năm")]
        [Range(1, 100, ErrorMessage = "Số năm phải từ 1 đến 100")]
        public int SoNam { get; set; }

        [Required(ErrorMessage = "Số ngày cộng thêm không được để trống")]
        [Display(Name = "Số ngày cộng thêm")]
        [Range(0, 30, ErrorMessage = "Số ngày cộng thêm phải từ 0 đến 30")]
        public int SoNgayCongThem { get; set; }

        [Required(ErrorMessage = "Năm áp dụng không được để trống")]
        [Display(Name = "Áp dụng từ năm")]
        [Range(2000, 2100, ErrorMessage = "Năm áp dụng phải từ 2000 đến 2100")]
        public int ApDungTuNam { get; set; }

        [Display(Name = "Còn hiệu lực")]
        public bool ConHieuLuc { get; set; }
    }

    public class ChinhSachPhepNamListViewModel
    {
        public List<ChinhSachPhepNamViewModel> ChinhSachPhepNams { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}