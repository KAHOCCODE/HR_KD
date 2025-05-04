using HR_KD.Data;

public class ChinhSachPhepNam
{
    public int Id { get; set; }
    public string TenChinhSach { get; set; }
    public int SoNam { get; set; }
    public int SoNgayCongThem { get; set; }
    public int ApDungTuNam { get; set; }
    public bool ConHieuLuc { get; set; }

    public ICollection<CauHinhPhep_ChinhSach> CauHinhPhep_ChinhSachs { get; set; }
}