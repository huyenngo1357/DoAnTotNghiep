using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class ChiSoDienNuocBusiness
    {
        public string SinhMa()
        {
            return "CS" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        // ✅ Thêm mới
        public ChiSoDienNuoc LayChiTiet(string maChiSo)
        {
            return DataProvider.Entities.ChiSoDienNuocs
                       .Include("HopDong")
                       .Include("HopDong.PhongTro")
                       .Include("HopDong.PhongTro.ToaNha")
                       .Include("HopDong.NguoiDung1")
                       .FirstOrDefault(x => x.MaChiSo == maChiSo
                                         && x.TrangThai == true);
        }

        public List<ChiSoDienNuoc> LayDanhSachChuTro(
            string maChuTro,
            string maHopDong,
            string loaiDichVu)
        {
            var lst = DataProvider.Entities.ChiSoDienNuocs
                          .Include("HopDong")
                          .Include("HopDong.PhongTro")
                          .Include("HopDong.PhongTro.ToaNha")
                          // ✅ NguoiDung1 = KhachHang
                          .Include("HopDong.NguoiDung1")
                          .Where(x => x.HopDong.MaChuTro == maChuTro
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(maHopDong))
                lst = lst.Where(x => x.MaHopDong == maHopDong);

            if (!string.IsNullOrEmpty(loaiDichVu))
                lst = lst.Where(x => x.LoaiDichVu == loaiDichVu);

            return lst.OrderByDescending(x => x.ThangGhi)
                      .ToList();
        }

        public List<HopDong> LayHopDongDangThue(string maChuTro)
        {
            return DataProvider.Entities.HopDongs
                       .Include("PhongTro")
                       .Include("PhongTro.ToaNha")
                       // ✅ NguoiDung1 = KhachHang
                       .Include("NguoiDung1")
                       .Where(x => x.MaChuTro == maChuTro
                                && x.TrangThaiHopDong == "DangThue"
                                && x.TrangThai == true)
                       .OrderBy(x => x.PhongTro.ToaNha.TenToaNha)
                       .ThenBy(x => x.PhongTro.SoPhong)
                       .ToList();
        }

        public ChiSoDienNuoc LayChiSoGanNhat(
            string maHopDong, string loaiDichVu)
        {
            return DataProvider.Entities.ChiSoDienNuocs
                       .Where(x => x.MaHopDong == maHopDong
                                && x.LoaiDichVu == loaiDichVu
                                && x.TrangThai == true)
                       .OrderByDescending(x => x.ThangGhi)
                       .FirstOrDefault();
        }

        public bool DaGhiThang(string maHopDong,
                                DateTime thangGhi)
        {
            return DataProvider.Entities.ChiSoDienNuocs
                       .Any(x => x.MaHopDong == maHopDong
                              && x.ThangGhi.Year == thangGhi.Year
                              && x.ThangGhi.Month == thangGhi.Month
                              && x.TrangThai == true);
        }

        public bool ThemMoiCaDienVaNuoc(
            ChiSoDienNuoc objDien,
            ChiSoDienNuoc objNuoc,
            HttpPostedFileBase anhDien,
            HttpPostedFileBase anhNuoc)
        {
            if (objDien == null || objNuoc == null)
                return false;

            if (DaGhiThang(objDien.MaHopDong,
                           objDien.ThangGhi))
                return false;

            var db = DataProvider.Entities;

            if (anhDien != null && anhDien.ContentLength > 0)
                objDien.AnhDongHo = LuuAnh(anhDien);
            if (anhNuoc != null && anhNuoc.ContentLength > 0)
                objNuoc.AnhDongHo = LuuAnh(anhNuoc);

            objDien.MaChiSo = SinhMa() + "D";
            objDien.LoaiDichVu = "Dien";
            objDien.TrangThai = true;
            objDien.NgayTao = DateTime.Now;

            objNuoc.MaChiSo = SinhMa() + "N";
            objNuoc.LoaiDichVu = "Nuoc";
            objNuoc.TrangThai = true;
            objNuoc.NgayTao = DateTime.Now;

            db.ChiSoDienNuocs.Add(objDien);
            db.ChiSoDienNuocs.Add(objNuoc);

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        // ✅ Sửa hoàn chỉnh
        public bool CapNhat(ChiSoDienNuoc obj,
                             HttpPostedFileBase anhMoi)
        {
            var db = DataProvider.Entities;
            var objDb = db.ChiSoDienNuocs
                           .FirstOrDefault(x =>
                               x.MaChiSo == obj.MaChiSo);
            if (objDb == null) return false;

            objDb.ChiSoDau = obj.ChiSoDau;
            objDb.ChiSoCuoi = obj.ChiSoCuoi;
            objDb.DonGia = obj.DonGia;
            objDb.ThangGhi = obj.ThangGhi;
            objDb.NgayCapNhat = DateTime.Now;

            if (anhMoi != null && anhMoi.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(objDb.AnhDongHo))
                    XoaAnhVatLy(objDb.AnhDongHo);
                objDb.AnhDongHo = LuuAnh(anhMoi);
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool Xoa(string maChiSo)
        {
            var db = DataProvider.Entities;
            var obj = db.ChiSoDienNuocs
                         .FirstOrDefault(x =>
                             x.MaChiSo == maChiSo);
            if (obj == null) return false;

            obj.TrangThai = false;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        private string LuuAnh(HttpPostedFileBase file)
        {
            string folder = HttpContext.Current.Server
                .MapPath("~/Content/images/dongho/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string tenFile = Guid.NewGuid()
                           + Path.GetExtension(file.FileName);
            file.SaveAs(Path.Combine(folder, tenFile));
            return tenFile;
        }

        private void XoaAnhVatLy(string tenFile)
        {
            string path = HttpContext.Current.Server
                .MapPath("~/Content/images/dongho/" + tenFile);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}