using HR_KD.Data;

public class CauHinhPhepNam
{
    public int Id { get; set; }
    public int Nam { get; set; }
    public int SoNgayPhepMacDinh { get; set; }

    public ICollection<CauHinhPhep_ChinhSach> CauHinhPhep_ChinhSachs { get; set; }
}