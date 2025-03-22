using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using HR_KD.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Data;

public partial class DaoTao
{
    public int MaDaoTao { get; set; }

    public string TenDaoTao { get; set; } = null!;

    public string? MoTa { get; set; }

    public string? NoiDung { get; set; }

    public DateOnly? NgayBatDau { get; set; }

    public DateOnly? NgayKetThuc { get; set; }

    public int MaPhongBan { get; set; }

    [ForeignKey("MaPhongBan")]
    public virtual PhongBan PhongBan { get; set; } = null!;

    public virtual ICollection<LichSuDaoTao> LichSuDaoTaos { get; set; } = new List<LichSuDaoTao>();
}
