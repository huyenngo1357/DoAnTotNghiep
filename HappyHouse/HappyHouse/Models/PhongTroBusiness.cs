using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class PhongTroBusiness
    {
        public PhongTro LayChiTiet(string maPhong)
        {
            return DataProvider.Entities.PhongTroes
                       .Include("ToaNha")
                       .Include("ToaNha.NguoiDung")
                       .Include("HinhAnhPhongs")
                       .Include("TienIches")
                       .FirstOrDefault(x => x.MaPhong == maPhong);
        }

        public string SinhMa()
        {
            return "P" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public List<TienIch> LayDanhSachTienIch()
        {
            return DataProvider.Entities.TienIches
                       .Where(x => x.TrangThai == true)
                       .OrderBy(x => x.TenTienIch)
                       .ToList();
        }

        public List<PhongTro> LayDanhSachChuTro(string maChuTro,
                                                 string tuKhoa,
                                                 string maToaNha,
                                                 string trangThaiPhong)
        {
            var lst = DataProvider.Entities.PhongTroes
                          .Include("ToaNha")
                          .Include("HinhAnhPhongs")
                          .Include("TienIches")
                          .Where(x => x.TrangThai == true
                                   && x.ToaNha.MaChuTro == maChuTro)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.SoPhong.Contains(tuKhoa)
                                  || x.ToaNha.TenToaNha.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maToaNha))
                lst = lst.Where(x => x.MaToaNha == maToaNha);

            if (!string.IsNullOrEmpty(trangThaiPhong))
                lst = lst.Where(x => x.TrangThaiPhong == trangThaiPhong);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        public List<ToaNha> LayToaNhaCuaChuTro(string maChuTro)
        {
            return DataProvider.Entities.ToaNhas
                       .Where(x => x.MaChuTro == maChuTro
                                && x.TrangThaiDuyet == "DaDuyet"
                                && x.TrangThai == true)
                       .OrderBy(x => x.TenToaNha)
                       .ToList();
        }

        public bool ThemMoi(PhongTro obj,
                            List<HttpPostedFileBase> dsHinhAnh,
                            List<string> dsTienIch)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            bool trung = db.PhongTroes
                            .Any(x => x.MaToaNha == obj.MaToaNha
                                   && x.SoPhong == obj.SoPhong
                                   && x.TrangThai == true);
            if (trung) return false;

            obj.MaPhong = SinhMa();
            obj.TrangThaiPhong = "Trong";
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            db.PhongTroes.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            if (dsTienIch != null && dsTienIch.Count > 0)
                LuuTienIch(obj.MaPhong, dsTienIch);

            if (dsHinhAnh != null && dsHinhAnh.Count > 0)
                LuuHinhAnh(obj.MaPhong, dsHinhAnh);

            return true;
        }

        public bool CapNhat(PhongTro obj,
                            List<HttpPostedFileBase> dsHinhAnh,
                            List<string> dsTienIch)
        {
            var db = DataProvider.Entities;
            var objDb = db.PhongTroes
                           .FirstOrDefault(x => x.MaPhong == obj.MaPhong);
            if (objDb == null) return false;

            bool trung = db.PhongTroes
                            .Any(x => x.MaToaNha == objDb.MaToaNha
                                   && x.SoPhong == obj.SoPhong
                                   && x.MaPhong != obj.MaPhong
                                   && x.TrangThai == true);
            if (trung) return false;

            objDb.SoPhong = obj.SoPhong;
            objDb.Tang = obj.Tang;
            objDb.DienTich = obj.DienTich;
            objDb.GiaThue = obj.GiaThue;
            objDb.TienCoc = obj.TienCoc;
            objDb.SoNguoiToiDa = obj.SoNguoiToiDa;
            objDb.MoTa = obj.MoTa;
            objDb.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            bool kq;
            try { kq = db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            if (dsTienIch != null)
            {
                var phong = db.PhongTroes
                               .Include("TienIches")
                               .FirstOrDefault(x => x.MaPhong == obj.MaPhong);
                if (phong != null)
                {
                    phong.TienIches.Clear();
                    db.Configuration.ValidateOnSaveEnabled = false;
                    try { db.SaveChanges(); }
                    finally { db.Configuration.ValidateOnSaveEnabled = true; }
                }
                if (dsTienIch.Count > 0)
                    LuuTienIch(obj.MaPhong, dsTienIch);
            }

            if (dsHinhAnh != null && dsHinhAnh.Count > 0)
                LuuHinhAnh(obj.MaPhong, dsHinhAnh);

            return kq;
        }

        public bool Xoa(string maPhong, string maChuTro)
        {
            var db = DataProvider.Entities;
            var obj = db.PhongTroes
                         .FirstOrDefault(x => x.MaPhong == maPhong
                                           && x.ToaNha.MaChuTro == maChuTro);
            if (obj == null) return false;

            bool dangThue = db.HopDongs
                               .Any(h => h.MaPhong == maPhong
                                      && h.TrangThaiHopDong == "DangThue");
            if (dangThue) return false;

            obj.TrangThai = false;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool XoaHinhAnh(int maHinhAnh)
        {
            var db = DataProvider.Entities;
            var hinh = db.HinhAnhPhongs
                          .FirstOrDefault(x => x.MaHinhAnh == maHinhAnh);
            if (hinh == null) return false;

            string path = HttpContext.Current.Server
                              .MapPath("~/Content/images/phongs/" + hinh.TenFile);
            if (File.Exists(path)) File.Delete(path);

            db.HinhAnhPhongs.Remove(hinh);
            db.SaveChanges();
            return true;
        }

        private void LuuHinhAnh(string maPhong,
                                 List<HttpPostedFileBase> dsHinhAnh)
        {
            var db = DataProvider.Entities;

            string folder = HttpContext.Current.Server
                                .MapPath("~/Content/images/phongs/");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            bool daCoDaiDien = db.HinhAnhPhongs
                                   .Any(x => x.MaPhong == maPhong
                                          && x.LaDaiDien == true);
            bool laDaiDien = !daCoDaiDien;

            foreach (var file in dsHinhAnh)
            {
                if (file == null || file.ContentLength == 0) continue;
                if (!file.ContentType.StartsWith("image")) continue;

                string tenFile = Guid.NewGuid() + Path.GetExtension(file.FileName);
                file.SaveAs(Path.Combine(folder, tenFile));

                db.HinhAnhPhongs.Add(new HinhAnhPhong
                {
                    MaPhong = maPhong,
                    TenFile = tenFile,
                    LaDaiDien = laDaiDien,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                });
                laDaiDien = false;
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        private void LuuTienIch(string maPhong, List<string> dsTienIch)
        {
            var db = DataProvider.Entities;
            var phong = db.PhongTroes
                           .Include("TienIches")
                           .FirstOrDefault(x => x.MaPhong == maPhong);
            if (phong == null) return;

            foreach (var maTienIch in dsTienIch)
            {
                if (string.IsNullOrEmpty(maTienIch)) continue;
                var ti = db.TienIches
                            .FirstOrDefault(x => x.MaTienIch == maTienIch);
                if (ti != null)
                    phong.TienIches.Add(ti);
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }
    }
}