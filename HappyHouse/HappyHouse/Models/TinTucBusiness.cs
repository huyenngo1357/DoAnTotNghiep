using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class TinTucBusiness
    {
        public List<TinTuc> LayDanhSach(string tuKhoa,
                                         string maChuDe,
                                         string trangThaiDang)
        {
            var lst = DataProvider.Entities.TinTucs
                          .Include("ChuDeTinTuc")
                          .Include("NguoiDung")
                          .Where(x => x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x =>
                    x.TieuDe.Contains(tuKhoa)
                 || (x.TomTat != null
                     && x.TomTat.Contains(tuKhoa)));

            if (!string.IsNullOrEmpty(maChuDe))
                lst = lst.Where(x => x.MaChuDe == maChuDe);

            if (!string.IsNullOrEmpty(trangThaiDang))
                lst = lst.Where(x => x.TrangThaiDang == trangThaiDang);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        public TinTuc LayChiTiet(string maTinTuc)
        {
            return DataProvider.Entities.TinTucs
                       .Include("ChuDeTinTuc")
                       .Include("NguoiDung")
                       .FirstOrDefault(x =>
                           x.MaTinTuc == maTinTuc
                        && x.TrangThai == true);
        }

        // FIX: đổi prefix "TT" → "TIN" để tránh trùng với ThanhToanBusiness ("TT...")
        public string SinhMa()
        {
            return "TIN" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public bool ThemMoi(TinTuc obj,
                             HttpPostedFileBase anhDaiDien)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            obj.MaTinTuc = SinhMa();
            obj.TrangThaiDang = "ChuaDang";
            obj.TrangThai = true;
            obj.LuotXem = 0;
            obj.NgayTao = DateTime.Now;

            if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
                obj.AnhDaiDien = LuuAnh(anhDaiDien);

            db.TinTucs.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool CapNhat(TinTuc obj,
                             HttpPostedFileBase anhDaiDien)
        {
            var db = DataProvider.Entities;
            var objDb = db.TinTucs
                           .FirstOrDefault(x =>
                               x.MaTinTuc == obj.MaTinTuc
                            && x.TrangThai == true);
            if (objDb == null) return false;

            objDb.TieuDe = obj.TieuDe;
            objDb.MaChuDe = obj.MaChuDe;
            objDb.NoiDung = obj.NoiDung;
            objDb.TomTat = obj.TomTat;
            objDb.NgayCapNhat = DateTime.Now;

            if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(objDb.AnhDaiDien))
                    XoaAnhVatLy(objDb.AnhDaiDien);
                objDb.AnhDaiDien = LuuAnh(anhDaiDien);
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool DangBai(string maTinTuc)
        {
            var db = DataProvider.Entities;
            var obj = db.TinTucs
                         .FirstOrDefault(x =>
                             x.MaTinTuc == maTinTuc
                          && x.TrangThai == true);
            if (obj == null) return false;
            if (obj.TrangThaiDang == "DaDang") return false;

            obj.TrangThaiDang = "DaDang";
            obj.NgayDang = DateTime.Now;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool TamAn(string maTinTuc)
        {
            var db = DataProvider.Entities;
            var obj = db.TinTucs
                         .FirstOrDefault(x =>
                             x.MaTinTuc == maTinTuc
                          && x.TrangThai == true);
            if (obj == null) return false;
            if (obj.TrangThaiDang != "DaDang") return false;

            obj.TrangThaiDang = "TamAn";
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool Xoa(string maTinTuc)
        {
            var db = DataProvider.Entities;
            var obj = db.TinTucs
                         .FirstOrDefault(x => x.MaTinTuc == maTinTuc);
            if (obj == null) return false;

            obj.TrangThai = false;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        // ── PRIVATE ──────────────────────────────────────────

        private string LuuAnh(HttpPostedFileBase file)
        {
            string folder = HttpContext.Current.Server
                .MapPath("~/Content/images/tintuc/");
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
                .MapPath("~/Content/images/tintuc/" + tenFile);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}