using HR_KD.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        /// Đặt lại trạng thái IsReset cho các bản ghi của năm trước
        /// </summary>
        public async Task ResetIsResetFlagForPreviousYearsAsync(int namHienTai)
        {
            // Set IsReset = false cho tất cả các bản ghi từ năm trước
            var phepNamCu = await _context.PhepNamNhanViens
                .Where(p => p.Nam < namHienTai && p.IsReset == true)
                .ToListAsync();

            foreach (var phep in phepNamCu)
            {
                phep.IsReset = false;
                phep.NgayCapNhat = DateTime.Now;
                phep.GhiChu = $"Đã chuyển IsReset về false khi sang năm {namHienTai}";
            }

            // Lưu thay đổi
            await _context.SaveChangesAsync();
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
                .Include(c => c.CauHinhPhep_ChinhSachs)
                .ThenInclude(cp => cp.ChinhSachPhepNam)
                .Where(c => c.Nam <= nam)  // Lọc các cấu hình cho năm <= năm hiện tại
                .OrderByDescending(c => c.Nam)  // Sắp xếp từ năm lớn nhất đến nhỏ nhất
                .FirstOrDefaultAsync();  // Lấy cấu hình gần nhất

            bool usedPreviousYearConfig = false;
            int configYear = nam;

            if (cauHinh == null)
            {
                // Nếu không tìm thấy cấu hình phép năm cho năm nào, tạo cấu hình mặc định
                cauHinh = new CauHinhPhepNam
                {
                    Nam = nam,
                    SoNgayPhepMacDinh = 12,  // Giá trị mặc định
                    CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>() // Khởi tạo danh sách rỗng
                };
                _context.CauHinhPhepNams.Add(cauHinh);
                await _context.SaveChangesAsync();
            }
            else if (cauHinh.Nam < nam)
            {
                // Nếu đang sử dụng cấu hình từ năm trước
                usedPreviousYearConfig = true;
                configYear = cauHinh.Nam;
            }

            // Đảm bảo danh sách chính sách không null
            if (cauHinh.CauHinhPhep_ChinhSachs == null)
            {
                cauHinh.CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>();
            }

            // Lấy tất cả nhân viên
            var nhanViens = await _context.NhanViens.ToListAsync();
            if (!nhanViens.Any())
                throw new InvalidOperationException("Không có nhân viên nào để áp dụng cấu hình.");

            foreach (var nv in nhanViens)
            {
                // Kiểm tra và tính thâm niên
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
                decimal soNgayPhepDuocCap = TinhSoNgayPhepDuocCap(nam, nv, cauHinh, thamnien.Value);

                // Kiểm tra và xử lý bản ghi trong PhepNamNhanVien
                var phepNam = await _context.PhepNamNhanViens
                    .SingleOrDefaultAsync(p => p.MaNv == nv.MaNv && p.Nam == nam);

                string ghiChu = $"Reset và áp dụng cấu hình năm {nam} cho {nv.HoTen ?? "Nhân viên không tên"}";
                if (usedPreviousYearConfig)
                {
                    ghiChu = $"Reset và áp dụng cấu hình từ năm {configYear} cho {nv.HoTen ?? "Nhân viên không tên"} (năm {nam} không có cấu hình riêng)";
                }

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
                        GhiChu = ghiChu
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
                    phepNam.GhiChu = ghiChu;
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
                .Where(c => c.Nam <= nam)
                .OrderByDescending(c => c.Nam)
                .FirstOrDefaultAsync();

            bool usedPreviousYearConfig = false;
            int configYear = nam;

            if (cauHinh == null)
            {
                // Nếu không tìm thấy cấu hình cho năm hiện tại, tạo mới
                cauHinh = new CauHinhPhepNam
                {
                    Nam = nam,
                    SoNgayPhepMacDinh = 12,  // Giá trị mặc định
                    CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>() // Khởi tạo danh sách rỗng
                };
                _context.CauHinhPhepNams.Add(cauHinh);
                await _context.SaveChangesAsync();
            }
            else if (cauHinh.Nam < nam)
            {
                usedPreviousYearConfig = true;
                configYear = cauHinh.Nam;
            }

            // Đảm bảo danh sách chính sách không null
            if (cauHinh.CauHinhPhep_ChinhSachs == null)
            {
                cauHinh.CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>();
            }

            // Tính thâm niên
            int thamnien = 0;
            if (nv.NgayVaoLam.HasValue && nv.NgayVaoLam.Value.Year <= nam)
            {
                thamnien = nam - nv.NgayVaoLam.Value.Year;
                if (thamnien < 0) thamnien = 0;
            }

            // Tính số ngày phép được cấp
            decimal soNgayPhepDuocCap = TinhSoNgayPhepDuocCap(nam, nv, cauHinh, thamnien);

            string ghiChu = $"Cập nhật phép cho nhân viên mới {nv.HoTen ?? "không rõ"} năm {nam}";
            if (usedPreviousYearConfig)
            {
                ghiChu = $"Cập nhật phép cho nhân viên mới {nv.HoTen ?? "không rõ"} năm {nam} (sử dụng cấu hình năm {configYear})";
            }

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
                GhiChu = ghiChu
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

        private decimal TinhSoNgayPhepDuocCap(int nam, NhanVien nv, CauHinhPhepNam cauHinh, int thamnien)
        {
            decimal soNgayPhep = cauHinh.SoNgayPhepMacDinh;

            // Áp dụng các chính sách thâm niên nếu có
            if (cauHinh.CauHinhPhep_ChinhSachs != null && cauHinh.CauHinhPhep_ChinhSachs.Any())
            {
                foreach (var cp in cauHinh.CauHinhPhep_ChinhSachs)
                {
                    // Kiểm tra null trước khi truy cập thuộc tính
                    if (cp.ChinhSachPhepNam != null &&
                        cp.ChinhSachPhepNam.ConHieuLuc &&
                        cp.ChinhSachPhepNam.ApDungTuNam <= nam &&
                        thamnien >= cp.ChinhSachPhepNam.SoNam)
                    {
                        soNgayPhep += cp.ChinhSachPhepNam.SoNgayCongThem;
                    }
                }
            }

            return soNgayPhep;
        }

        public async Task UpdateSoNgayDaSuDungAsync(int maNgayNghi)
        {
            // Lấy thông tin đơn nghỉ phép
            var ngayNghi = await _context.NgayNghis
                .Include(n => n.MaLoaiNgayNghiNavigation) // Include LoaiNgayNghi để kiểm tra TinhVaoPhepNam
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

            // Kiểm tra xem loại ngày nghỉ có tính vào phép năm không
            if (ngayNghi.MaLoaiNgayNghiNavigation == null || !ngayNghi.MaLoaiNgayNghiNavigation.TinhVaoPhepNam)
            {
                return; // Không tính vào phép năm, không cần xử lý tiếp
            }

            // Lấy bản ghi PhepNamNhanVien của nhân viên cho năm tương ứng
            var phepNam = await _context.PhepNamNhanViens
                .FirstOrDefaultAsync(p => p.MaNv == ngayNghi.MaNv && p.Nam == ngayNghi.NgayNghi1.Year);

            if (phepNam == null)
            {
                // Nếu chưa có bản ghi, tạo mới với cấu hình mặc định
                var cauHinh = await _context.CauHinhPhepNams
                    .Include(c => c.CauHinhPhep_ChinhSachs)
                    .ThenInclude(cp => cp.ChinhSachPhepNam)
                    .Where(c => c.Nam <= ngayNghi.NgayNghi1.Year)
                    .OrderByDescending(c => c.Nam)
                    .FirstOrDefaultAsync();

                bool usedPreviousYearConfig = false;
                int configYear = ngayNghi.NgayNghi1.Year;

                if (cauHinh == null)
                {
                    cauHinh = new CauHinhPhepNam
                    {
                        Nam = ngayNghi.NgayNghi1.Year,
                        SoNgayPhepMacDinh = 12, // Giá trị mặc định
                        CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>() // Khởi tạo danh sách rỗng
                    };
                    _context.CauHinhPhepNams.Add(cauHinh);
                    await _context.SaveChangesAsync();
                }
                else if (cauHinh.Nam < ngayNghi.NgayNghi1.Year)
                {
                    usedPreviousYearConfig = true;
                    configYear = cauHinh.Nam;
                }

                // Đảm bảo danh sách chính sách không null
                if (cauHinh.CauHinhPhep_ChinhSachs == null)
                {
                    cauHinh.CauHinhPhep_ChinhSachs = new List<CauHinhPhep_ChinhSach>();
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
                decimal soNgayPhepDuocCap = TinhSoNgayPhepDuocCap(ngayNghi.NgayNghi1.Year, nhanVien, cauHinh, thamnien);

                string ghiChu = $"Tạo bản ghi phép năm cho đơn nghỉ phép {maNgayNghi}";
                if (usedPreviousYearConfig)
                {
                    ghiChu = $"Tạo bản ghi phép năm cho đơn nghỉ phép {maNgayNghi} (sử dụng cấu hình năm {configYear})";
                }

                // Tạo bản ghi mới
                phepNam = new PhepNamNhanVien
                {
                    MaNv = ngayNghi.MaNv,
                    Nam = ngayNghi.NgayNghi1.Year,
                    SoNgayPhepDuocCap = soNgayPhepDuocCap,
                    SoNgayDaSuDung = 0,
                    NgayCapNhat = DateTime.Now,
                    CauHinhPhepNamId = cauHinh.Id,
                    IsReset = false, // Bản ghi được tạo trong năm không phải từ quá trình reset
                    GhiChu = ghiChu
                };
                _context.PhepNamNhanViens.Add(phepNam);
            }

            // Tính số ngày nghỉ (giả định mỗi đơn là 1 ngày, có thể điều chỉnh tùy theo logic nghiệp vụ)
            decimal soNgayNghi = 1; // Cần tính toán chính xác số ngày nghỉ dựa trên đơn

            // Cập nhật số ngày đã sử dụng
            phepNam.SoNgayDaSuDung += soNgayNghi;
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