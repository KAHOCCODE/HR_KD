namespace HR_KD.DTOs
{
    public class RejectAttendanceRequestDTO
    {
        public int MaYeuCau { get; set; }
        public DateOnly NgayLamViec { get; set; }
        public string? LyDo { get; set; }
        public int? MaLoaiNgayNghi { get; set; }
    }
}
