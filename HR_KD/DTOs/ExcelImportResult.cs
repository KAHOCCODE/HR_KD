namespace HR_KD.DTOs
{
    public class ExcelImportResult
    {
        public List<ImportNhanVienDto> Employees { get; set; } = new();
        public Dictionary<int, string> Errors { get; set; } = new();
    }
}
