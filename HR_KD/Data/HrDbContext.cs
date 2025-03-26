using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HR_KD.Data;

public partial class HrDbContext : DbContext
{
    public HrDbContext()
    {
    }

    public HrDbContext(DbContextOptions<HrDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BangLuong> BangLuongs { get; set; }

    public virtual DbSet<CauHinhLuongThue> CauHinhLuongThues { get; set; }

    public virtual DbSet<ChamCong> ChamCongs { get; set; }

    public virtual DbSet<ChiTietDanhGium> ChiTietDanhGia { get; set; }

    public virtual DbSet<ChucVu> ChucVus { get; set; }

    public virtual DbSet<DanhGiaNhanVien> DanhGiaNhanViens { get; set; }

    public virtual DbSet<DaoTao> DaoTaos { get; set; }

    public virtual DbSet<LichSuChamCong> LichSuChamCongs { get; set; }

    public virtual DbSet<LichSuDaoTao> LichSuDaoTaos { get; set; }

    public virtual DbSet<LoaiNgayNghi> LoaiNgayNghis { get; set; }

    public virtual DbSet<NgayLe> NgayLes { get; set; }

    public virtual DbSet<NgayNghi> NgayNghis { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<PhongBan> PhongBans { get; set; }

    public virtual DbSet<QuyenHan> QuyenHans { get; set; }

    public virtual DbSet<SalaryStatistic> SalaryStatistics { get; set; }

    public virtual DbSet<SoDuPhep> SoDuPheps { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TaiKhoanNganHang> TaiKhoanNganHangs { get; set; }

    public virtual DbSet<TangCa> TangCas { get; set; }

    public virtual DbSet<ThongBao> ThongBaos { get; set; }

    public virtual DbSet<TieuChiDanhGiaFullTime> TieuChiDanhGiaFullTimes { get; set; }

    public virtual DbSet<TieuChiDanhGiaPartTime> TieuChiDanhGiaPartTimes { get; set; }

    public virtual DbSet<YeuCauSuaChamCong> YeuCauSuaChamCongs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=LAPTOP-QBAUIQJE\\Piemon;Initial Catalog=QuanLyNhanSu;Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BangLuong>(entity =>
        {
            entity.HasKey(e => e.MaLuong).HasName("PK__BangLuon__6609A48DC1CE160C");

            entity.ToTable("BangLuong");

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.LuongCoBan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.PhuCap)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThuNhapKhac)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongLuong)
                .HasComputedColumnSql("(([LuongCoBan]+[PhuCap])+[ThuNhapKhac])", false)
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa Thanh Toán");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.BangLuongs)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__BangLuong__MaNV__60A75C0F");
        });

        modelBuilder.Entity<CauHinhLuongThue>(entity =>
        {
            entity.HasKey(e => e.MaCauHinh).HasName("PK__CauHinhL__F0685B7DE11C888F");

            entity.ToTable("CauHinhLuongThue");

            entity.Property(e => e.GiaTri).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenCauHinh).HasMaxLength(100);
        });

        modelBuilder.Entity<ChamCong>(entity =>
        {
            entity.HasKey(e => e.MaChamCong).HasName("PK__ChamCong__307331A168A5D6B5");

            entity.ToTable("ChamCong", tb =>
                {
                    tb.HasTrigger("trg_InsertLichSuChamCong");
                    tb.HasTrigger("trg_UpdateTongGio");
                });

            entity.Property(e => e.GhiChu).HasMaxLength(255);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.TongGio).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa Duyệt");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.ChamCongs)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChamCong__MaNV__48CFD27E");
        });

        modelBuilder.Entity<ChiTietDanhGium>(entity =>
        {
            entity.HasKey(e => e.MaChiTietDanhGia).HasName("PK__ChiTietD__CC6E45534F93654C");

            entity.Property(e => e.DiemDanhGia).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.NhanXet).HasMaxLength(255);

            entity.HasOne(d => d.MaDanhGiaNavigation).WithMany(p => p.ChiTietDanhGia)
                .HasForeignKey(d => d.MaDanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietDa__MaDan__6EF57B66");

            entity.HasOne(d => d.MaTieuChiDanhGiaNavigation).WithMany(p => p.ChiTietDanhGia)
                .HasForeignKey(d => d.MaTieuChiDanhGia)
                .HasConstraintName("FK__ChiTietDa__MaTie__6FE99F9F");
        });

        modelBuilder.Entity<ChucVu>(entity =>
        {
            entity.HasKey(e => e.MaChucVu).HasName("PK__ChucVu__D4639533E0783BE9");

            entity.ToTable("ChucVu");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenChucVu).HasMaxLength(100);
        });

        modelBuilder.Entity<DanhGiaNhanVien>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGiaN__AA9515BF62111141");

            entity.ToTable("DanhGiaNhanVien");

            entity.Property(e => e.DiemDanhGia).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NhanXet).HasMaxLength(255);

            entity.HasOne(d => d.MaNguoiDanhGiaNavigation).WithMany(p => p.DanhGiaNhanViens)
                .HasForeignKey(d => d.MaNguoiDanhGia)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__DanhGiaNh__MaNgu__656C112C");
        });

        modelBuilder.Entity<DaoTao>(entity =>
        {
            entity.HasKey(e => e.MaDaoTao).HasName("PK__DaoTao__81987A7CE02F6E17");

            entity.ToTable("DaoTao");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.NoiDung).HasMaxLength(225);
            entity.Property(e => e.TenDaoTao).HasMaxLength(100);
            entity.HasOne(d => d.PhongBan)
                  .WithMany(p => p.DaoTaos)
                  .HasForeignKey(d => d.MaPhongBan)
                  .OnDelete(DeleteBehavior.Cascade);

        });

        modelBuilder.Entity<LichSuChamCong>(entity =>
        {
            entity.HasKey(e => e.MaLichSuChamCong).HasName("PK__LichSuCh__66853896CB759516");

            entity.ToTable("LichSuChamCong");

            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.TongGio).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa Duyệt");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.LichSuChamCongs)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LichSuCham__MaNV__4CA06362");
        });

        modelBuilder.Entity<LichSuDaoTao>(entity =>
        {
            entity.HasKey(e => e.MaLichSu).HasName("PK__LichSuDa__C443222A500F5F13");

            entity.ToTable("LichSuDaoTao");

            entity.Property(e => e.KetQua).HasMaxLength(255);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");

            entity.HasOne(d => d.MaDaoTaoNavigation).WithMany(p => p.LichSuDaoTaos)
                .HasForeignKey(d => d.MaDaoTao)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LichSuDao__MaDao__7C4F7684");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.LichSuDaoTaos)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LichSuDaoT__MaNV__7B5B524B");
        });

        modelBuilder.Entity<LoaiNgayNghi>(entity =>
        {
            entity.HasKey(e => e.MaLoaiNgayNghi).HasName("PK__LoaiNgay__3E18383F70879B53");

            entity.ToTable("LoaiNgayNghi");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenLoai).HasMaxLength(100);
        });

        modelBuilder.Entity<NgayLe>(entity =>
        {
            entity.HasKey(e => e.MaNgayLe).HasName("PK__NgayLe__55131492858AED82");

            entity.ToTable("NgayLe");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.NgayLe1).HasColumnName("NgayLe");
            entity.Property(e => e.TenNgayLe).HasMaxLength(100);
        });

        modelBuilder.Entity<NgayNghi>(entity =>
        {
            entity.HasKey(e => e.MaNgayNghi).HasName("PK__NgayNghi__526A7F783CFC0001");

            entity.ToTable("NgayNghi", tb => tb.HasTrigger("trg_UpdateSoDuPhep"));

            entity.Property(e => e.LyDo).HasMaxLength(255);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayNghi1).HasColumnName("NgayNghi");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa Duyệt");

            entity.HasOne(d => d.MaLoaiNgayNghiNavigation).WithMany(p => p.NgayNghis)
                .HasForeignKey(d => d.MaLoaiNgayNghi)
                .HasConstraintName("FK__NgayNghi__MaLoai__5165187F");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.NgayNghis)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NgayNghi__MaNV__5070F446");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70A275D256C");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.Email, "UQ__NhanVien__A9D10534AE786C7D").IsUnique();

            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.DiaChi).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .HasColumnName("SDT");
            entity.Property(e => e.TrinhDoHocVan).HasMaxLength(100);

            entity.HasOne(d => d.MaChucVuNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.MaChucVu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhanVien__MaChuc__3D5E1FD2");

            entity.HasOne(d => d.MaPhongBanNavigation).WithMany(p => p.NhanViens)
                .HasForeignKey(d => d.MaPhongBan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhanVien__MaPhon__3C69FB99");
        });

        modelBuilder.Entity<PhongBan>(entity =>
        {
            entity.HasKey(e => e.MaPhongBan).HasName("PK__PhongBan__D0910CC8615D7A9E");

            entity.ToTable("PhongBan");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenPhongBan).HasMaxLength(100);
        });

        modelBuilder.Entity<QuyenHan>(entity =>
        {
            entity.HasKey(e => e.MaQuyenHan).HasName("PK__QuyenHan__3EAF3EE647B2787F");

            entity.ToTable("QuyenHan");

            entity.Property(e => e.MaQuyenHan).HasMaxLength(50);
            entity.Property(e => e.MoTaQuyenHan).HasMaxLength(255);
            entity.Property(e => e.TenQuyenHan).HasMaxLength(255);
        });

        modelBuilder.Entity<SalaryStatistic>(entity =>
        {
            entity.HasKey(e => e.MaThongKe).HasName("PK__SalarySt__60E521F4040BFD95");

            entity.Property(e => e.TongBhxh)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TongBHXH");
            entity.Property(e => e.TongLuong).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongThucNhan).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TongTncn)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TongTNCN");
        });

        modelBuilder.Entity<SoDuPhep>(entity =>
        {
            entity.HasKey(e => new { e.MaNv, e.MaLoaiNgayNghi, e.Nam }).HasName("PK__SoDuPhep__070385985BD7438C");

            entity.ToTable("SoDuPhep");

            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.SoNgayConLai).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.MaLoaiNgayNghiNavigation).WithMany(p => p.SoDuPheps)
                .HasForeignKey(d => d.MaLoaiNgayNghi)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SoDuPhep__MaLoai__5535A963");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.SoDuPheps)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SoDuPhep__MaNV__5441852A");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Username).HasName("PK__TaiKhoan__536C85E5C4E86E6E");

            entity.ToTable("TaiKhoan");

            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.MaQuyenHan).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaiKhoan__MaNV__4222D4EF");

            entity.HasOne(d => d.MaQuyenHanNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaQuyenHan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaiKhoan__MaQuye__4316F928");
        });

        modelBuilder.Entity<TaiKhoanNganHang>(entity =>
        {
            entity.HasKey(e => e.MaTk).HasName("PK__TaiKhoan__27250070144A40C2");

            entity.ToTable("TaiKhoanNganHang");

            entity.HasIndex(e => e.SoTaiKhoan, "UQ__TaiKhoan__E619229B413F90EC").IsUnique();

            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.ChiNhanh).HasMaxLength(100);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.SoTaiKhoan).HasMaxLength(50);
            entity.Property(e => e.TenNganHang).HasMaxLength(100);

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.TaiKhoanNganHangs)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaiKhoanNg__MaNV__5AEE82B9");
        });

        modelBuilder.Entity<TangCa>(entity =>
        {
            entity.HasKey(e => e.MaTangCa).HasName("PK__TangCa__B3E1C5497A40A099");

            entity.ToTable("TangCa");

            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.SoGioTangCa).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa Duyệt");
            entity.Property(e => e.TyLeTangCa).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.TangCas)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TangCa__MaNV__693CA210");
        });

        modelBuilder.Entity<ThongBao>(entity =>
        {
            entity.HasKey(e => e.MaThongBao).HasName("PK__ThongBao__04DEB54EF0F2502F");

            entity.ToTable("ThongBao");

            entity.Property(e => e.DaDoc).HasDefaultValue(false);
            entity.Property(e => e.LoaiThongBao).HasMaxLength(50);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayThongBao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TieuDe).HasMaxLength(255);

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.ThongBaos)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("FK__ThongBao__MaNV__01142BA1");
        });

        modelBuilder.Entity<TieuChiDanhGiaFullTime>(entity =>
        {
            entity.HasKey(e => e.MaTieuChiDanhGia).HasName("PK__TieuChiD__F90598A0F25A7704");

            entity.ToTable("TieuChiDanhGiaFullTime");

            entity.Property(e => e.DiemDanhGia).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.MoTaDanhGia).HasMaxLength(255);
            entity.Property(e => e.TenDanhGia).HasMaxLength(255);

            entity.HasOne(d => d.MaDanhGiaNavigation).WithMany(p => p.TieuChiDanhGiaFullTimes)
                .HasForeignKey(d => d.MaDanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TieuChiDa__MaDan__6C190EBB");
        });

        modelBuilder.Entity<TieuChiDanhGiaPartTime>(entity =>
        {
            entity.HasKey(e => e.MaTieuChiDanhGia).HasName("PK__TieuChiD__F90598A054481BC4");

            entity.ToTable("TieuChiDanhGiaPartTime");

            entity.Property(e => e.DiemDanhGia).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.MoTaDanhGia).HasMaxLength(255);
            entity.Property(e => e.TenDanhGia).HasMaxLength(255);
        });

        modelBuilder.Entity<YeuCauSuaChamCong>(entity =>
        {
            entity.HasKey(e => e.MaYeuCau).HasName("PK__YeuCauSu__CFA5DF4EB14CB029");

            entity.ToTable("YeuCauSuaChamCong");

            entity.Property(e => e.LyDo).HasMaxLength(255);
            entity.Property(e => e.MaNv).HasColumnName("MaNV");
            entity.Property(e => e.NgayYeuCau)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.YeuCauSuaChamCongs)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__YeuCauSuaC__MaNV__76969D2E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
