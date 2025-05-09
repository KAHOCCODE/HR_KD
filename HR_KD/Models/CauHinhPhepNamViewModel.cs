using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HR_KD.ViewModels
{
    public class CauHinhPhepNamViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm")]
        [Display(Name = "Năm")]
        public int Nam { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số ngày phép mặc định")]
        [Display(Name = "Số ngày phép mặc định")]
        [Range(0, 50, ErrorMessage = "Số ngày phép phải từ 0 đến 50")]
        public int SoNgayPhepMacDinh { get; set; }

        [Display(Name = "Chính sách phép năm")]
        public List<int> ChinhSachPhepNamIds { get; set; } = new List<int>();

        // Danh sách để hiển thị tất cả chính sách trong dropdown/checkbox
        public List<ChinhSachPhepNamListItem> DanhSachChinhSach { get; set; } = new List<ChinhSachPhepNamListItem>();
        public bool IsCurrentYear { get; set; }
    }

    public class ChinhSachPhepNamListItem
    {
        public int Id { get; set; }
        public string TenChinhSach { get; set; }
        public int SoNam { get; set; }
        public int SoNgayCongThem { get; set; }
        public int ApDungTuNam { get; set; }
        public bool ConHieuLuc { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CauHinhPhepNamListViewModel
    {
        public List<CauHinhPhepNamItemViewModel> DanhSachCauHinh { get; set; } = new List<CauHinhPhepNamItemViewModel>();
    }

    public class CauHinhPhepNamItemViewModel
    {
        public int Id { get; set; }
        public int Nam { get; set; }
        public int SoNgayPhepMacDinh { get; set; }
        public bool IsCurrentYear { get; set; }
        public List<string> DanhSachTenChinhSach { get; set; } = new List<string>();
    }
}