using HR_KD.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HR_KD.Services
{
    public class PhepNamService
    {
        private readonly HrDbContext _context;

        public PhepNamService(HrDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tự động reset và tính toán lại số ngày phép cho tất cả nhân viên vào đầu năm mới.
        /// </summary>
        public async Task AutoResetAndCalculatePhepNamAsync(int nam)
        {
            // Kiểm tra ngày hiện tại để đảm bảo chỉ chạy vào ngày 1/1
            if (DateTime.Now.Month != 1 || DateTime.Now.Day != 1)
                return; // Chỉ chạy vào ngày 1/1, tránh chạy nhầm

            var cauHinh = await _context.CauHinhPhepNams
                .Where(c => c.Nam <= nam)  // Lọc các cấu hình cho năm <= năm hiện tại
                .OrderByDescending(c => c.Nam)  // Sắp xếp từ năm lớn nhất đến nhỏ nhất
                .FirstOrDefaultAsync();  // Lấy cấu hình gần nhất

            if (cauHinh == null)
            {
                // Nếu không tìm thấy cấu hình phép năm cho năm gần nhất, tạo cấu hình mặc định
                cauHinh = new CauHinhPhepNam
                {
                    Nam = nam,
                    SoNgayPhepMacDinh = 12  // Giá trị mặc định
                };
                _context.CauHinhPhepNams.Add(cauHinh);
                await _context.SaveChangesAsync();
            }

            // Lấy tất cả nhân viên
            var nhanViens = await _context.NhanViens.ToListAsync();
            if (!nhanViens.Any())
                throw new InvalidOperationException("Không có nhân viên nào để áp dụng cấu hình.");

            foreach (var nv in nhanViens)
            {
                // Kiểm tra và tính thâm niên từ DateOnly?
                int? thamnien = null;
                if (nv.NgayVaoLam.HasValue)
                {
                    thamnien = nam - nv.NgayVaoLam.Value.Year;
                }

                // Nếu không có ngày vào làm hoặc thâm niên không hợp lệ, sử dụng thâm niên mặc định (0)
                if (!thamnien.HasValue || thamnien < 0)
                {
                    thamnien = 0;
                }

                // Tính số ngày phép được cấp
                decimal soNgayPhepDuocCap = await TinhSoNgayPhepDuocCap(nam, nv, cauHinh, thamnien.Value);

                // Kiểm tra và xử lý bản ghi trong PhepNamNhanVien
                var phepNam = await _context.PhepNamNhanViens
                    .SingleOrDefaultAsync(p => p.MaNv == nv.MaNv && p.Nam == nam);

                if (phepNam == null)
                {
                    // Tạo bản ghi mới
                    phepNam = new PhepNamNhanVien
                    {
                        MaNv = nv.MaNv,
                        Nam = nam,
                        SoNgayPhepDuocCap = soNgayPhepDuocCap,
                        SoNgayDaSuDung = 0,
                        NgayCapNhat = DateTime.Now,
                        CauHinhPhepNamId = cauHinh.Id,
                        IsReset = true,
                        GhiChu = $"Reset và áp dụng cấu hình năm {nam} cho {nv.HoTen ?? "Nhân viên không tên"}"
                    };
                    _context.PhepNamNhanViens.Add(phepNam);
                }
                else
                {
                    // Reset và cập nhật
                    phepNam.SoNgayPhepDuocCap = soNgayPhepDuocCap;
                    phepNam.SoNgayDaSuDung = 0; // Reset số ngày đã sử dụng
                    phepNam.NgayCapNhat = DateTime.Now;
                    phepNam.CauHinhPhepNamId = cauHinh.Id;
                    phepNam.IsReset = true;
                    phepNam.GhiChu = $"Reset và cập nhật cấu hình năm {nam} cho {nv.HoTen ?? "Nhân viên không tên"}";
                }

                // Cập nhật hoặc tạo bản ghi trong SoDuPhep
                var soDuPhep = await _context.SoDuPheps
                    .SingleOrDefaultAsync(s => s.MaNv == nv.MaNv && s.Nam == nam);

                if (soDuPhep == null)
                {
                    soDuPhep = new SoDuPhep
                    {
                        MaNv = nv.MaNv,
                        Nam = nam,
                        SoNgayConLai = soNgayPhepDuocCap,
                        NgayCapNhat = DateTime.Now
                    };
                    _context.SoDuPheps.Add(soDuPhep);
                }
                else
                {
                    soDuPhep.SoNgayConLai = soNgayPhepDuocCap - phepNam.SoNgayDaSuDung;
                    soDuPhep.NgayCapNhat = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task CapNhatPhepChoNhanVienMoiAsync(string maNv, int nam)
        {
            // Tìm nhân viên theo mã (ép kiểu MaNv về string để so sánh)
            var nv = await _context.NhanViens
                .SingleOrDefaultAsync(n => n.MaNv.ToString() == maNv);

            if (nv == null)
                throw new InvalidOperationException("Không tìm thấy nhân viên.");

            // Kiểm tra xem đã có bản ghi phép năm chưa
            var daCoPhep = await _context.PhepNamNhanViens
                .AnyAsync(p => p.MaNv.ToString() == maNv && p.Nam == nam);
            if (daCoPhep)
                return; // Đã có phép năm thì không cập nhật nữa

            // Lấy cấu hình phép năm
            var cauHinh = await _context.CauHinhPhepNams
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .ThenInclude(cp => cp.ChinhSachPhepNam)
                .SingleOrDefaultAsync(c => c.Nam == nam);

            if (cauHinh == null)
                throw new InvalidOperationException($"Không tìm thấy cấu hình phép năm cho năm {nam}.");

            // Tính thâm niên
            int thamnien = 0;
            if (nv.NgayVaoLam.HasValue && nv.NgayVaoLam.Value.Year <= nam)
            {
                thamnien = nam - nv.NgayVaoLam.Value.Year;
                if (thamnien < 0) thamnien = 0;
            }

            // Tính số ngày phép được cấp
            decimal soNgayPhepDuocCap = await TinhSoNgayPhepDuocCap(nam, nv, cauHinh, thamnien);

            // Tạo bản ghi phép năm
            var phepNam = new PhepNamNhanVien
            {
                MaNv = nv.MaNv,
                Nam = nam,
                SoNgayPhepDuocCap = soNgayPhepDuocCap,
                SoNgayDaSuDung = 0,
                NgayCapNhat = DateTime.Now,
                CauHinhPhepNamId = cauHinh.Id,
                IsReset = true,
                GhiChu = $"Cập nhật phép cho nhân viên mới {nv.HoTen ?? "không rõ"} năm {nam}"
            };
            _context.PhepNamNhanViens.Add(phepNam);

            // Tạo bản ghi số dư phép
            var soDuPhep = new SoDuPhep
            {
                MaNv = nv.MaNv,
                Nam = nam,
                SoNgayConLai = soNgayPhepDuocCap,
                NgayCapNhat = DateTime.Now
            };
            _context.SoDuPheps.Add(soDuPhep);

            await _context.SaveChangesAsync();
        }

        private async Task<decimal> TinhSoNgayPhepDuocCap(int nam, NhanVien nv, CauHinhPhepNam cauHinh, int thamnien)
        {
            decimal soNgayPhep = cauHinh.SoNgayPhepMacDinh;

            // Áp dụng các chính sách thâm niên
            foreach (var cp in cauHinh.CauHinhPhep_ChinhSachs)
            {
                var chinhSach = cp.ChinhSachPhepNam;
                if (chinhSach.ConHieuLuc &&
                    chinhSach.ApDungTuNam <= nam &&
                    thamnien >= chinhSach.SoNam)
                {
                    soNgayPhep += chinhSach.SoNgayCongThem;
                }
            }

            return soNgayPhep;
        }

        public async Task UpdateSoNgayDaSuDungAsync(int maNgayNghi)
        {
            // Lấy thông tin đơn nghỉ phép
            var ngayNghi = await _context.NgayNghis
                .FirstOrDefaultAsync(n => n.MaNgayNghi == maNgayNghi);

            if (ngayNghi == null)
            {
                throw new InvalidOperationException("Đơn nghỉ phép không tồn tại.");
            }

            // Giả định MaTrangThai = "NN2" là "Đã duyệt"
            if (ngayNghi.MaTrangThai != "NN2")
            {
                return; // Chỉ xử lý nếu đơn đã được duyệt
            }

            // Lấy bản ghi PhepNamNhanVien của nhân viên cho năm tương ứng
            var phepNam = await _context.PhepNamNhanViens
                .FirstOrDefaultAsync(p => p.MaNv == ngayNghi.MaNv && p.Nam == ngayNghi.NgayNghi1.Year);

            if (phepNam == null)
            {
                // Nếu chưa có bản ghi, tạo mới với cấu hình mặc định
                var cauHinh = await _context.CauHinhPhepNams
                    .Where(c => c.Nam <= ngayNghi.NgayNghi1.Year)
                    .OrderByDescending(c => c.Nam)
                    .FirstOrDefaultAsync();

                if (cauHinh == null)
                {
                    cauHinh = new CauHinhPhepNam
                    {
                        Nam = ngayNghi.NgayNghi1.Year,
                        SoNgayPhepMacDinh = 12 // Giá trị mặc định
                    };
                    _context.CauHinhPhepNams.Add(cauHinh);
                    await _context.SaveChangesAsync();
                }

                // Lấy thông tin nhân viên
                var nhanVien = await _context.NhanViens
                    .FirstOrDefaultAsync(n => n.MaNv == ngayNghi.MaNv);

                if (nhanVien == null)
                {
                    throw new InvalidOperationException("Nhân viên không tồn tại.");
                }

                // Tính thâm niên
                int thamnien = nhanVien.NgayVaoLam.HasValue
                    ? ngayNghi.NgayNghi1.Year - nhanVien.NgayVaoLam.Value.Year
                    : 0;
                if (thamnien < 0) thamnien = 0;

                // Tính số ngày phép được cấp
                decimal soNgayPhepDuocCap = await TinhSoNgayPhepDuocCap(ngayNghi.NgayNghi1.Year, nhanVien, cauHinh, thamnien);

                // Tạo bản ghi mới
                phepNam = new PhepNamNhanVien
                {
                    MaNv = ngayNghi.MaNv,
                    Nam = ngayNghi.NgayNghi1.Year,
                    SoNgayPhepDuocCap = soNgayPhepDuocCap,
                    SoNgayDaSuDung = 0,
                    NgayCapNhat = DateTime.Now,
                    CauHinhPhepNamId = cauHinh.Id,
                    IsReset = false,
                    GhiChu = $"Tạo bản ghi phép năm cho đơn nghỉ phép {maNgayNghi}"
                };
                _context.PhepNamNhanViens.Add(phepNam);
            }

            // Cập nhật số ngày đã sử dụng (giả định mỗi đơn là 1 ngày)
            phepNam.SoNgayDaSuDung += 1;
            phepNam.NgayCapNhat = DateTime.Now;
            phepNam.GhiChu = $"Cập nhật số ngày đã sử dụng cho đơn nghỉ phép {maNgayNghi}";

            // Tính số ngày còn lại
            decimal soNgayConLai = phepNam.SoNgayPhepDuocCap - phepNam.SoNgayDaSuDung;

            // Cập nhật hoặc tạo bản ghi trong SoDuPhep
            var soDuPhep = await _context.SoDuPheps
                .SingleOrDefaultAsync(s => s.MaNv == ngayNghi.MaNv && s.Nam == ngayNghi.NgayNghi1.Year);

            if (soDuPhep == null)
            {
                soDuPhep = new SoDuPhep
                {
                    MaNv = ngayNghi.MaNv,
                    Nam = ngayNghi.NgayNghi1.Year,
                    SoNgayConLai = soNgayConLai,
                    NgayCapNhat = DateTime.Now
                };
                _context.SoDuPheps.Add(soDuPhep);
            }
            else
            {
                soDuPhep.SoNgayConLai = soNgayConLai;
                soDuPhep.NgayCapNhat = DateTime.Now;
            }

            // Lưu thay đổi
            await _context.SaveChangesAsync();
        }
    }
}