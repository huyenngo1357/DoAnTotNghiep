using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHouse.Models
{
    public class HoaDonBusiness
    {
        public HoaDon LayChiTiet(string maHoaDon)
        {
            return DataProvider.Entities.HoaDons
                       .Include("HopDong")
                       .Include("HopDong.PhongTro")
                       .Include("HopDong.PhongTro.ToaNha")
                       .Include("HopDong.NguoiDung1")
                       .Include("HopDong.HopDong_DichVu")
                       .Include("HopDong.HopDong_DichVu.GiaDichVu")
                       .Include("HopDong.HopDong_DichVu.GiaDichVu.TienIch")
                       .Include("ChiSoDienNuoc")
                       .Include("ChiSoDienNuoc1")
                       .Include("ThanhToans")
                       .FirstOrDefault(x => x.MaHoaDon == maHoaDon);
        }

        public string SinhMa()
        {
            return "HOA" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public List<HoaDon> LayDanhSachChuTro(string maChuTro,
                                               string tuKhoa,
                                               string maHopDong,
                                               string trangThai)
        {
            var lst = DataProvider.Entities.HoaDons
                          .Include("HopDong")
                          .Include("HopDong.PhongTro")
                          .Include("HopDong.PhongTro.ToaNha")
                          .Include("HopDong.NguoiDung1")
                          .Where(x => x.HopDong.MaChuTro == maChuTro
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x =>
                    x.MaHoaDon.Contains(tuKhoa)
                 || x.HopDong.NguoiDung1.HoTen.Contains(tuKhoa)
                 || x.HopDong.PhongTro.SoPhong.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maHopDong))
                lst = lst.Where(x => x.MaHopDong == maHopDong);

            if (!string.IsNullOrEmpty(trangThai))
                lst = lst.Where(x => x.TrangThaiHoaDon == trangThai);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        public List<HopDong> LayHopDongDangThueCuaChuTro(string maChuTro)
        {
            return DataProvider.Entities.HopDongs
                       .Include("PhongTro")
                       .Include("PhongTro.ToaNha")
                       .Include("NguoiDung1")
                       .Include("HopDong_DichVu")
                       .Include("HopDong_DichVu.GiaDichVu")
                       .Include("HopDong_DichVu.GiaDichVu.TienIch")
                       .Where(x => x.MaChuTro == maChuTro
                                && x.TrangThaiHopDong == "DangThue"
                                && x.TrangThai == true)
                       .OrderBy(x => x.PhongTro.ToaNha.TenToaNha)
                       .ThenBy(x => x.PhongTro.SoPhong)
                       .ToList();
        }

        // Lấy thông tin chi tiết để tạo hóa đơn
        public HoaDonThongTinDto LayThongTinTaoHoaDon(
                                    string maHopDong,
                                    string thangHoaDon)
        {
            var hopDong = DataProvider.Entities.HopDongs
                              .Include("PhongTro")
                              .Include("PhongTro.ToaNha")
                              .Include("HopDong_DichVu")
                              .Include("HopDong_DichVu.GiaDichVu")
                              .Include("HopDong_DichVu.GiaDichVu.TienIch")
                              .FirstOrDefault(x => x.MaHopDong == maHopDong);

            if (hopDong == null) return null;

            DateTime thang;
            if (!DateTime.TryParse(thangHoaDon + "-01", out thang))
                thang = DateTime.Today;

            // Chỉ số điện tháng được chọn
            var chiSoDien = DataProvider.Entities.ChiSoDienNuocs
                                .Where(x => x.MaHopDong == maHopDong
                                         && x.LoaiDichVu == "Dien"
                                         && x.ThangGhi.Month == thang.Month
                                         && x.ThangGhi.Year == thang.Year
                                         && x.TrangThai == true)
                                .FirstOrDefault();

            // Chỉ số nước tháng được chọn
            var chiSoNuoc = DataProvider.Entities.ChiSoDienNuocs
                                .Where(x => x.MaHopDong == maHopDong
                                         && x.LoaiDichVu == "Nuoc"
                                         && x.ThangGhi.Month == thang.Month
                                         && x.ThangGhi.Year == thang.Year
                                         && x.TrangThai == true)
                                .FirstOrDefault();

            // Dịch vụ từ HopDong_DichVu
            var dsDichVu = hopDong.HopDong_DichVu
                               .Select(dv => new DichVuHoaDonDto
                               {
                                   MaGiaDichVu = dv.MaGiaDichVu,
                                   TenDichVu = dv.GiaDichVu?.TienIch?.TenTienIch
                                                 ?? dv.GiaDichVu?.TenDichVu ?? "",
                                   BieuTuong = dv.GiaDichVu?.TienIch?.BieuTuong
                                                 ?? "fa-cog",
                                   DonGia = dv.GiaDichVu?.DonGia ?? 0,
                                   DonVi = dv.GiaDichVu?.DonVi ?? "tháng",
                                   SoLuong = dv.SoLuong,
                                   ThanhTien = (dv.GiaDichVu?.DonGia ?? 0)
                                                 * dv.SoLuong
                               })
                               .ToList();

            decimal tongDichVu = dsDichVu.Sum(x => x.ThanhTien);

            // Tính hạn thanh toán
            int ngayTT = hopDong.NgayThanhToanHangThang;
            DateTime hanTT;
            try
            {
                hanTT = new DateTime(thang.Year, thang.Month, ngayTT);
                if (hanTT < DateTime.Today)
                    hanTT = hanTT.AddMonths(1);
            }
            catch
            {
                hanTT = new DateTime(thang.Year, thang.Month, 1)
                            .AddMonths(1).AddDays(-1);
            }

            return new HoaDonThongTinDto
            {
                TienPhong = hopDong.GiaThueThang,
                // Điện
                TienDien = chiSoDien?.ThanhTien ?? 0,
                MaChiSoDien = chiSoDien?.MaChiSo,
                SoTieuThuDien = chiSoDien?.SoTieuThu ?? 0,
                DonGiaDien = chiSoDien?.DonGia ?? 0,
                CoDien = chiSoDien != null,
                // Nước
                TienNuoc = chiSoNuoc?.ThanhTien ?? 0,
                MaChiSoNuoc = chiSoNuoc?.MaChiSo,
                SoTieuThuNuoc = chiSoNuoc?.SoTieuThu ?? 0,
                DonGiaNuoc = chiSoNuoc?.DonGia ?? 0,
                CoNuoc = chiSoNuoc != null,
                // Dịch vụ
                DsDichVu = dsDichVu,
                TongDichVu = tongDichVu,
                // Tổng
                HanThanhToan = hanTT.ToString("yyyy-MM-dd")
            };
        }

        public bool ThemMoi(HoaDon obj)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            bool daCoHoaDon = db.HoaDons
                                 .Any(x => x.MaHopDong == obj.MaHopDong
                                        && x.ThangHoaDon.Year == obj.ThangHoaDon.Year
                                        && x.ThangHoaDon.Month == obj.ThangHoaDon.Month
                                        && x.TrangThai == true);
            if (daCoHoaDon) return false;

            obj.MaHoaDon = SinhMa();
            obj.TrangThaiHoaDon = "ChuaThanhToan";
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            obj.TongTien = obj.TienPhong
                         + obj.TienDien
                         + obj.TienNuoc
                         + obj.TienDichVu;

            db.HoaDons.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool CapNhat(HoaDon obj)
        {
            var db = DataProvider.Entities;
            var objDb = db.HoaDons
                           .FirstOrDefault(x => x.MaHoaDon == obj.MaHoaDon);
            if (objDb == null) return false;
            if (objDb.TrangThaiHoaDon == "DaThanhToan") return false;

            objDb.TienPhong = obj.TienPhong;
            objDb.TienDien = obj.TienDien;
            objDb.TienNuoc = obj.TienNuoc;
            objDb.TienDichVu = obj.TienDichVu;
            objDb.HanThanhToan = obj.HanThanhToan;
            objDb.GhiChu = obj.GhiChu;
            objDb.MaChiSoDien = obj.MaChiSoDien;
            objDb.MaChiSoNuoc = obj.MaChiSoNuoc;
            objDb.NgayCapNhat = DateTime.Now;

            objDb.TongTien = objDb.TienPhong
                           + objDb.TienDien
                           + objDb.TienNuoc
                           + objDb.TienDichVu;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool XacNhanThanhToan(string maHoaDon)
        {
            var db = DataProvider.Entities;
            var obj = db.HoaDons.FirstOrDefault(x => x.MaHoaDon == maHoaDon);
            if (obj == null) return false;
            if (obj.TrangThaiHoaDon == "DaThanhToan") return false;

            obj.TrangThaiHoaDon = "DaThanhToan";
            obj.NgayThanhToan = DateTime.Now;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool DanhDauQuaHan(string maHoaDon)
        {
            var db = DataProvider.Entities;
            var obj = db.HoaDons.FirstOrDefault(x => x.MaHoaDon == maHoaDon);
            if (obj == null) return false;
            if (obj.TrangThaiHoaDon != "ChuaThanhToan") return false;

            obj.TrangThaiHoaDon = "QuaHan";
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public int DemChuaThanhToan(string maChuTro)
        {
            return DataProvider.Entities.HoaDons
                       .Count(x => x.HopDong.MaChuTro == maChuTro
                                && (x.TrangThaiHoaDon == "ChuaThanhToan"
                                 || x.TrangThaiHoaDon == "QuaHan"
                                 || x.TrangThaiHoaDon == "ChoDuyet")
                                && x.TrangThai == true);
        }
    }

    // ── DTOs 

    public class HoaDonThongTinDto
    {
        public decimal TienPhong { get; set; }
        public decimal TienDien { get; set; }
        public string MaChiSoDien { get; set; }
        public decimal? SoTieuThuDien { get; set; }
        public decimal DonGiaDien { get; set; }
        public bool CoDien { get; set; }
        public decimal TienNuoc { get; set; }
        public string MaChiSoNuoc { get; set; }
        public decimal? SoTieuThuNuoc { get; set; }
        public decimal DonGiaNuoc { get; set; }
        public bool CoNuoc { get; set; }
        public List<DichVuHoaDonDto> DsDichVu { get; set; }
        public decimal TongDichVu { get; set; }
        public string HanThanhToan { get; set; }
    }

    public class DichVuHoaDonDto
    {
        public string MaGiaDichVu { get; set; }
        public string TenDichVu { get; set; }
        public string BieuTuong { get; set; }
        public decimal DonGia { get; set; }
        public string DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }
}