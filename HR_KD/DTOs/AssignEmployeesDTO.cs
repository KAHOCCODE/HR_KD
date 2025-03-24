namespace HR_KD.DTOs
{
    public class AssignEmployeesDTO
    {
        public int MaDaoTao { get; set; }
        public List<int> MaNvs { get; set; } // Danh sách MaNv để gán
    }
}