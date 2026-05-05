using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHouse.Models
{
    public class GiaDichVuBusiness
    {
        public GiaDichVu LayChiTiet(string maGiaDichVu)
        {
            return DataProvider.Entities.GiaDichVus
                       .Include("ToaNha")
                       .Include("TienIch")
                       .FirstOrDefault(x => x.MaGiaDichVu == maGiaDichVu);
        }

        public string SinhMa()
        {
            return "GDV" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public List<GiaDichVu> LayDanhSachChuTro(string maChuTro,
                                                   string maToaNha = null,
                                                   string maTienIch = null)
        {
            var lst = DataProvider.Entities.GiaDichVus
                          .Include("ToaNha")
                          .Include("TienIch")
                          .Where(x => x.ToaNha.MaChuTro == maChuTro
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(maToaNha))
                lst = lst.Where(x => x.MaToaNha == maToaNha);

            if (!string.IsNullOrEmpty(maTienIch))
                lst = lst.Where(x => x.MaTienIch == maTienIch);

            return lst.OrderBy(x => x.MaToaNha)
                      .ThenBy(x => x.TienIch.TenTienIch)
                      .ToList();
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

        public List<GiaDichVu> LayDichVuTheoToaNha(string maToaNha)
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

        public bool ThemMoi(GiaDichVu obj)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            obj.MaGiaDichVu = SinhMa();
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            db.GiaDichVus.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool CapNhat(GiaDichVu obj)
        {
            var db = DataProvider.Entities;
            var objDb = db.GiaDichVus
                           .FirstOrDefault(x => x.MaGiaDichVu == obj.MaGiaDichVu);
            if (objDb == null) return false;

            objDb.TenDichVu = obj.TenDichVu;
            objDb.DonVi = obj.DonVi;
            objDb.DonGia = obj.DonGia;
            objDb.NgayApDung = obj.NgayApDung;
            objDb.NgayKetThuc = obj.NgayKetThuc;
            objDb.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool DoiTrangThai(string maGiaDichVu)
        {
            var db = DataProvider.Entities;
            var obj = db.GiaDichVus
                         .FirstOrDefault(x => x.MaGiaDichVu == maGiaDichVu);
            if (obj == null) return false;

            obj.TrangThai = !obj.TrangThai;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool Xoa(string maGiaDichVu, string maChuTro)
        {
            var db = DataProvider.Entities;
            var obj = db.GiaDichVus
                         .Include("ToaNha")
                         .FirstOrDefault(x => x.MaGiaDichVu == maGiaDichVu
                                           && x.ToaNha.MaChuTro == maChuTro);
            if (obj == null) return false;

            obj.TrangThai = false;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        // Lấy giá hiện tại theo MaTienIch (thay vì LoaiDichVu cũ)
        public GiaDichVu LayGiaHienTai(string maToaNha, string maTienIch)
        {
            return DataProvider.Entities.GiaDichVus
                       .Include("TienIch")
                       .Where(x => x.MaToaNha == maToaNha
                                && x.MaTienIch == maTienIch
                                && x.TrangThai == true
                                && x.NgayApDung <= System.DateTime.Today
                                && (x.NgayKetThuc == null
                                    || x.NgayKetThuc >= System.DateTime.Today))
                       .OrderByDescending(x => x.NgayApDung)
                       .FirstOrDefault();
        }
    }
}