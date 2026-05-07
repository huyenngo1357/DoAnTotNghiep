using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHouse.Models
{
    public class HopDongBusiness
    {
        public HopDong LayChiTiet(string maHopDong)
        {
            return DataProvider.Entities.HopDongs
                       .Include("PhongTro")
                       .Include("PhongTro.ToaNha")
                       .Include("NguoiDung")    // ChuTro
                       .Include("NguoiDung1")   // KhachHang
                       .Include("HopDong_DichVu")
                       .Include("HopDong_DichVu.GiaDichVu")
                       .Include("HopDong_DichVu.GiaDichVu.TienIch")
                       .FirstOrDefault(x => x.MaHopDong == maHopDong);
        }

        public string SinhMa()
        {
            return "HD" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public List<HopDong> LayDanhSachChuTro(string maChuTro,
                                                string tuKhoa,
                                                string maPhong,
                                                string trangThai)
        {
            var lst = DataProvider.Entities.HopDongs
                          .Include("PhongTro")
                          .Include("PhongTro.ToaNha")
                          .Include("HopDong_DichVu")
                          .Include("HopDong_DichVu.GiaDichVu")
                          .Include("HopDong_DichVu.GiaDichVu.TienIch")
                          .Include("NguoiDung1")   // KhachHang — FK MaKhachHang
                          .Where(x => x.MaChuTro == maChuTro
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.MaHopDong.Contains(tuKhoa)
                                  || x.NguoiDung1.HoTen.Contains(tuKhoa)
                                  || x.NguoiDung1.SoDienThoai.Contains(tuKhoa)
                                  || x.PhongTro.SoPhong.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maPhong))
                lst = lst.Where(x => x.MaPhong == maPhong);

            if (!string.IsNullOrEmpty(trangThai))
                lst = lst.Where(x => x.TrangThaiHopDong == trangThai);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        // Phòng trống theo tòa nhà
        public List<PhongTro> LayPhongTrongTheoToaNha(string maToaNha)
        {
            return DataProvider.Entities.PhongTroes
                       .Where(x => x.MaToaNha == maToaNha
                                && x.TrangThaiPhong == "Trong"
                                && x.TrangThai == true)
                       .OrderBy(x => x.Tang)
                       .ThenBy(x => x.SoPhong)
                       .ToList();
        }

        // Tòa nhà đã duyệt của chủ trọ
        public List<ToaNha> LayToaNhaCuaChuTro(string maChuTro)
        {
            return DataProvider.Entities.ToaNhas
                       .Where(x => x.MaChuTro == maChuTro
                                && x.TrangThaiDuyet == "DaDuyet"
                                && x.TrangThai == true)
                       .OrderBy(x => x.TenToaNha)
                       .ToList();
        }

        // Tìm kiếm khách hàng realtime
        public List<NguoiDung> TimKiemKhachHang(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa))
                return new List<NguoiDung>();

            tuKhoa = tuKhoa.Trim();
            return DataProvider.Entities.NguoiDungs
                       .Where(x => x.MaVaiTro == "KHACHHANG"
                                && x.TrangThai == true
                                && (x.HoTen.Contains(tuKhoa)
                                 || x.SoDienThoai.Contains(tuKhoa)
                                 || x.Email.Contains(tuKhoa)))
                       .OrderBy(x => x.HoTen)
                       .Take(10)
                       .ToList();
        }

        // Lấy dịch vụ tính tiền của tòa nhà
        public List<GiaDichVu> LayDichVuToaNha(string maToaNha)
        {
            return DataProvider.Entities.GiaDichVus
                       .Include("TienIch")
                       .Where(x => x.MaToaNha == maToaNha
                                && x.TrangThai == true
                                && x.NgayApDung <= DateTime.Today
                                && (x.NgayKetThuc == null
                                    || x.NgayKetThuc >= DateTime.Today))
                       .OrderBy(x => x.TienIch.TenTienIch)
                       .ToList();
        }

        // Lấy dịch vụ đang gắn với hợp đồng
        public List<HopDong_DichVu> LayDichVuHopDong(string maHopDong)
        {
            return DataProvider.Entities.HopDong_DichVu
                       .Include("GiaDichVu")
                       .Include("GiaDichVu.TienIch")
                       .Where(x => x.MaHopDong == maHopDong)
                       .ToList();
        }

        public bool ThemMoi(HopDong obj,
                             List<string> dsMaGiaDichVu,
                             List<int> dsSoLuong)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            // FIX: kiểm tra cả "ChoKy" lẫn "DangThue"
            // Nếu chỉ check "DangThue" thì có thể tạo nhiều hợp đồng
            // "ChoKy" cho cùng 1 phòng
            bool dangBan = db.HopDongs
                              .Any(x => x.MaPhong == obj.MaPhong
                                     && (x.TrangThaiHopDong == "DangThue"
                                      || x.TrangThaiHopDong == "ChoKy")
                                     && x.TrangThai == true);
            if (dangBan) return false;

            obj.MaHopDong = SinhMa();
            obj.TrangThaiHopDong = "ChoKy";
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            db.HopDongs.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            // Lưu dịch vụ kèm theo
            LuuDichVu(obj.MaHopDong, dsMaGiaDichVu, dsSoLuong);

            return true;
        }

        public bool CapNhat(HopDong obj,
                             List<string> dsMaGiaDichVu,
                             List<int> dsSoLuong)
        {
            var db = DataProvider.Entities;
            var objDb = db.HopDongs
                           .FirstOrDefault(x => x.MaHopDong == obj.MaHopDong);
            if (objDb == null) return false;
            if (objDb.TrangThaiHopDong != "ChoKy") return false;

            objDb.MaKhachHang = obj.MaKhachHang;
            objDb.NgayBatDau = obj.NgayBatDau;
            objDb.NgayKetThuc = obj.NgayKetThuc;
            objDb.GiaThueThang = obj.GiaThueThang;
            objDb.TienCoc = obj.TienCoc;
            objDb.NgayThanhToanHangThang = obj.NgayThanhToanHangThang;
            objDb.DieuKhoan = obj.DieuKhoan;
            objDb.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            bool kq;
            try { kq = db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            if (kq)
            {
                // Xóa dịch vụ cũ rồi lưu lại
                var dvCu = db.HopDong_DichVu
                              .Where(x => x.MaHopDong == obj.MaHopDong)
                              .ToList();
                db.HopDong_DichVu.RemoveRange(dvCu);
                db.Configuration.ValidateOnSaveEnabled = false;
                try { db.SaveChanges(); }
                finally { db.Configuration.ValidateOnSaveEnabled = true; }

                LuuDichVu(obj.MaHopDong, dsMaGiaDichVu, dsSoLuong);
            }

            return kq;
        }

        public bool LuuDichVu(string maHopDong,
                               List<string> dsMaGiaDichVu,
                               List<int> dsSoLuong)
        {
            if (dsMaGiaDichVu == null || dsMaGiaDichVu.Count == 0)
                return true;

            var db = DataProvider.Entities;

            for (int i = 0; i < dsMaGiaDichVu.Count; i++)
            {
                string maDV = dsMaGiaDichVu[i];
                if (string.IsNullOrEmpty(maDV)) continue;

                int soLuong = (dsSoLuong != null && i < dsSoLuong.Count)
                              ? dsSoLuong[i] : 1;
                if (soLuong < 1) soLuong = 1;

                db.HopDong_DichVu.Add(new HopDong_DichVu
                {
                    MaHopDong = maHopDong,
                    MaGiaDichVu = maDV,
                    SoLuong = soLuong,
                    GhiChu = null
                });
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() >= 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool KyHopDong(string maHopDong)
        {
            var db = DataProvider.Entities;
            var hd = db.HopDongs
                        .Include("PhongTro")
                        .FirstOrDefault(x => x.MaHopDong == maHopDong);

            if (hd == null || hd.TrangThaiHopDong != "ChoKy")
                return false;

            // 1. Ký hợp đồng
            hd.TrangThaiHopDong = "DangThue";
            hd.NgayCapNhat = DateTime.Now;

            // 2. Cập nhật phòng → DaThue
            if (hd.PhongTro != null)
                hd.PhongTro.TrangThaiPhong = "DaThue";

            // 3. AUTO SINH HÓA ĐƠN CỌC — không cần sửa DB
            if (hd.TienCoc.HasValue && hd.TienCoc.Value > 0)
            {
                // Kiểm tra chưa tồn tại hóa đơn cọc
                bool daCo = db.HoaDons
                               .Any(x => x.MaHopDong == maHopDong
                                       && x.MaHoaDon.StartsWith("COC")
                                       && x.TrangThai == true);
                if (!daCo)
                {
                    db.HoaDons.Add(new HoaDon
                    {
                        // Prefix "COC" = dấu hiệu nhận biết hóa đơn cọc
                        MaHoaDon = "COC" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        MaHopDong = maHopDong,

                        // Tháng = NgayBatDau - 1 tháng
                        // → tránh xung đột UNIQUE INDEX (MaHopDong, ThangHoaDon)
                        ThangHoaDon = hd.NgayBatDau.AddMonths(-1),

                        // Toàn bộ số tiền dồn vào TienPhong
                        TienPhong = hd.TienCoc.Value,
                        TienDien = 0,
                        TienNuoc = 0,
                        TienDichVu = 0,
                        TongTien = hd.TienCoc.Value,

                        HanThanhToan = hd.NgayBatDau.AddDays(7),
                        GhiChu = "Tiền đặt cọc",
                        TrangThaiHoaDon = "ChuaThanhToan",
                        TrangThai = true,
                        NgayTao = DateTime.Now
                    });
                }
            }

            // 4. Thông báo khách
            db.ThongBaos.Add(new ThongBao
            {
                MaNguoiDung = hd.MaKhachHang,
                TieuDe = "Hợp đồng đã được ký xác nhận",
                NoiDung = hd.TienCoc > 0
                    ? $"Hợp đồng đã ký. Vui lòng thanh toán tiền cọc "
                      + $"{hd.TienCoc.Value:N0}đ trước "
                      + $"{hd.NgayBatDau.AddDays(7):dd/MM/yyyy}."
                    : "Hợp đồng của bạn đã được ký.",
                LoaiThongBao = "HopDong",
                DuongDan = "/ThanhToan/DanhSach",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool HuyHopDong(string maHopDong, string lyDo)
        {
            var db = DataProvider.Entities;
            var hopDong = db.HopDongs
                             .FirstOrDefault(x => x.MaHopDong == maHopDong);
            if (hopDong == null) return false;
            if (hopDong.TrangThaiHopDong == "DaHuy"
             || hopDong.TrangThaiHopDong == "HetHan") return false;

            string cu = hopDong.TrangThaiHopDong;
            hopDong.TrangThaiHopDong = "DaHuy";
            hopDong.NgayHuy = DateTime.Now;
            hopDong.LyDoHuy = lyDo;
            hopDong.NgayCapNhat = DateTime.Now;

            // Chỉ trả phòng về Trong nếu đang thuê
            // Nếu là ChoKy thì phòng vẫn Trong, không cần đổi
            if (cu == "DangThue")
            {
                var phong = db.PhongTroes
                               .FirstOrDefault(x => x.MaPhong == hopDong.MaPhong);
                if (phong != null)
                {
                    phong.TrangThaiPhong = "Trong";
                    phong.NgayCapNhat = DateTime.Now;
                }
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool KetThucHopDong(string maHopDong)
        {
            var db = DataProvider.Entities;
            var hopDong = db.HopDongs
                             .FirstOrDefault(x => x.MaHopDong == maHopDong);
            if (hopDong == null) return false;
            if (hopDong.TrangThaiHopDong != "DangThue") return false;

            hopDong.TrangThaiHopDong = "HetHan";
            hopDong.NgayCapNhat = DateTime.Now;

            var phong = db.PhongTroes
                           .FirstOrDefault(x => x.MaPhong == hopDong.MaPhong);
            if (phong != null)
            {
                phong.TrangThaiPhong = "Trong";
                phong.NgayCapNhat = DateTime.Now;
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }
    }
}