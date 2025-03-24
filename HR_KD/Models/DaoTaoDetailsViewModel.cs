using HR_KD.Data;
using HR_KD.DTOs;

namespace HR_KD.Models
{
    public class DaoTaoDetailsViewModel
    {
        public DaoTao DaoTao { get; set; }
        public List<LichSuDaoTaoDTO> LichSuDaoTaos { get; set; }
    }
}
