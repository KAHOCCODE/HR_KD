namespace HR_KD.Data
{
    public class ChamCongGioRaVao
    {

        public int Id { get; set; }
        public TimeOnly GioVao { get; set; }
        public TimeOnly GioRa { get; set; } 
        public bool KichHoat { get; set; } = false;
        public decimal TongGio { get; set; }
    }
}
