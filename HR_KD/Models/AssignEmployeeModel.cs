using HR_KD.Data;
using HR_KD.DTOs;

namespace HR_KD.Models
{
    public class AssignEmployeeModel
    {
        public int MaDaoTao { get; set; }
        public List<int> MaNvs { get; set; }
    }
}
