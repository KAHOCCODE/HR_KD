namespace HR_KD.Data
{
    public class ChamCongGioRaVao
    {

        public int Id { get; set; }
        public DateTime GioVao { get; set; }
        public DateTime GioRa { get; set; } 
        public bool KichHoat { get; set; } = false;
        public decimal TongGio { get; set; }
    }
}
