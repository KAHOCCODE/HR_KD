using static HR_KD.ApiControllers.AttendanceController;

namespace HR_KD.DTOs
{
    public class AcceptAttendanceRequestDTO
    {
        public int MaYeuCau { get; set; }
        public List<ChamCongRequestDTO> AttendanceData { get; set; }
    }
}
