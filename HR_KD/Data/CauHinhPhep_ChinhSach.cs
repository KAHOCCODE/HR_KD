namespace HR_KD.Data
{
    public class CauHinhPhep_ChinhSach
    {
        public int CauHinhPhepNamId { get; set; }
        public int ChinhSachPhepNamId { get; set; }
        public virtual CauHinhPhepNam CauHinhPhepNam { get; set; }
        public virtual ChinhSachPhepNam ChinhSachPhepNam { get; set; }
    }
}