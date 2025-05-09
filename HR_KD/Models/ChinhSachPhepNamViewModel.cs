using System;
using System.ComponentModel.DataAnnotations;

namespace HR_KD.Models
{
    public class ChinhSachPhepNamViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chính sách")]
        [Display(Name = "Tên chính sách")]
        [StringLength(100, ErrorMessage = "Tên chính sách không được quá 100 ký tự")]
        public string TenChinhSach { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số năm")]
        [Display(Name = "Số năm")]
        [Range(1, 50, ErrorMessage = "Số năm phải từ 1 đến 50")]
        public int SoNam { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số ngày cộng thêm")]
        [Display(Name = "Số ngày cộng thêm")]
        [Range(1, 20, ErrorMessage = "Số ngày cộng thêm phải từ 1 đến 20")]
        public int SoNgayCongThem { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm áp dụng")]
        [Display(Name = "Áp dụng từ năm")]
        [Range(2000, 2100, ErrorMessage = "Năm áp dụng phải từ 2000 đến 2100")]
        public int ApDungTuNam { get; set; }

        [Display(Name = "Còn hiệu lực")]
        public bool ConHieuLuc { get; set; }

        // Thêm thuộc tính để kiểm tra chính sách có đang được sử dụng không
        [Display(Name = "Đang sử dụng")]
        public bool DangSuDung { get; set; }
    }

    public class ChinhSachPhepNamListViewModel
    {
        public System.Collections.Generic.List<ChinhSachPhepNamViewModel> ChinhSachPhepNams { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}