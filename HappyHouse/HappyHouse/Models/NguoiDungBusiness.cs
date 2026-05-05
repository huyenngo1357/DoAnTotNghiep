using HappyHouse.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class NguoiDungBusiness
    {
        // ── DÙNG CHUNG ───────────────────────────────────────────────

        public NguoiDung LayChiTiet(string maNguoiDung)
        {
            return DataProvider.Entities.NguoiDungs
                       .Include("VaiTro")
                       .FirstOrDefault(x => x.MaNguoiDung == maNguoiDung);
        }

        public string SinhMa()
        {
            return "ND" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        // ── ADMIN ────────────────────────────────────────────────────

        public List<NguoiDung> LayDanhSach(string tuKhoa,
                                            string maVaiTro,
                                            bool? trangThai)
        {
            var lst = DataProvider.Entities.NguoiDungs
                          .Include("VaiTro")
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.HoTen.Contains(tuKhoa)
                                  || x.Email.Contains(tuKhoa)
                                  || x.SoDienThoai.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(maVaiTro))
                lst = lst.Where(x => x.MaVaiTro == maVaiTro);

            if (trangThai.HasValue)
                lst = lst.Where(x => x.TrangThai == trangThai.Value);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        public List<VaiTro> LayDanhSachVaiTro()
        {
            return DataProvider.Entities.VaiTroes.ToList();
        }

        public bool ThemMoi(NguoiDung obj, HttpPostedFileBase anhDaiDien)
        {
            if (obj == null) return false;
            var db = DataProvider.Entities;  // ✅ 1 instance

            bool emailTonTai = db.NguoiDungs
                                  .Any(x => x.Email == obj.Email);
            if (emailTonTai) return false;

            bool sdtTonTai = db.NguoiDungs
                                .Any(x => x.SoDienThoai == obj.SoDienThoai);
            if (sdtTonTai) return false;

            obj.MaNguoiDung = SinhMa();
            obj.MatKhau = Common.HashPassword(obj.MatKhau);
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
                obj.AnhDaiDien = LuuAnhDaiDien(anhDaiDien);

            db.NguoiDungs.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool CapNhat(NguoiDung obj, HttpPostedFileBase anhDaiDien)
        {
            var db = DataProvider.Entities;  // ✅ 1 instance
            var objDb = db.NguoiDungs
                           .FirstOrDefault(x =>
                               x.MaNguoiDung == obj.MaNguoiDung);
            if (objDb == null) return false;

            bool emailTrung = db.NguoiDungs
                                 .Any(x => x.Email == obj.Email
                                        && x.MaNguoiDung != obj.MaNguoiDung);
            if (emailTrung) return false;

            bool sdtTrung = db.NguoiDungs
                               .Any(x => x.SoDienThoai == obj.SoDienThoai
                                      && x.MaNguoiDung != obj.MaNguoiDung);
            if (sdtTrung) return false;

            objDb.HoTen = obj.HoTen;
            objDb.Email = obj.Email;
            objDb.SoDienThoai = obj.SoDienThoai;
            objDb.ZaloPhone = obj.ZaloPhone;
            objDb.MaVaiTro = obj.MaVaiTro;
            objDb.SoCCCD = obj.SoCCCD;
            objDb.DiaChi = obj.DiaChi;
            objDb.GioiTinh = obj.GioiTinh;
            objDb.NgaySinh = obj.NgaySinh;
            objDb.TrangThai = obj.TrangThai;
            objDb.NgayCapNhat = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(obj.MatKhau))
                objDb.MatKhau = Common.HashPassword(obj.MatKhau);

            if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(objDb.AnhDaiDien))
                    XoaAnhVatLy(objDb.AnhDaiDien);
                objDb.AnhDaiDien = LuuAnhDaiDien(anhDaiDien);
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool DoiTrangThai(string maNguoiDung)
        {
            var db = DataProvider.Entities;
            var obj = db.NguoiDungs
                         .FirstOrDefault(x =>
                             x.MaNguoiDung == maNguoiDung);
            if (obj == null) return false;

            obj.TrangThai = !obj.TrangThai;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        // ── PRIVATE ─────────────────────────────────────────────────

        private string LuuAnhDaiDien(HttpPostedFileBase file)
        {
            string folder = HttpContext.Current.Server
                                .MapPath("~/Content/images/avatars/");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string tenFile = Guid.NewGuid() + Path.GetExtension(file.FileName);
            file.SaveAs(Path.Combine(folder, tenFile));
            return tenFile;
        }

        private void XoaAnhVatLy(string tenFile)
        {
            string duongDan = HttpContext.Current.Server
                                  .MapPath("~/Content/images/avatars/" + tenFile);
            if (File.Exists(duongDan)) File.Delete(duongDan);
        }
    }
}