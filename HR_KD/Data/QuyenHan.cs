using System;
using System.Collections.Generic;
using System.Text.Json;

namespace HR_KD.Data;

public partial class QuyenHan
{
    public string MaQuyenHan { get; set; } = null!;

    public string TenQuyenHan { get; set; } = null!;

    public string MoTaQuyenHan { get; set; } = null!;

    public string? QuyenChiTiet { get; set; } = null!;

    public virtual ICollection<TaiKhoanQuyenHan> TaiKhoanQuyenHans { get; set; } = new List<TaiKhoanQuyenHan>();

    // Phương thức hỗ trợ lấy quyền chi tiết
    public Dictionary<string, Dictionary<string, bool>> GetPermissions()
    {
        if (string.IsNullOrEmpty(QuyenChiTiet))
            return new Dictionary<string, Dictionary<string, bool>>();

        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(QuyenChiTiet)
               ?? new Dictionary<string, Dictionary<string, bool>>();
    }

    // Phương thức hỗ trợ cập nhật quyền chi tiết
    public void SetPermissions(Dictionary<string, Dictionary<string, bool>> permissions)
    {
        QuyenChiTiet = JsonSerializer.Serialize(permissions);
    }

}
