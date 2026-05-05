using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHouse.Models
{
    public class ChuDeTinTucBusiness
    {
        public List<ChuDeTinTuc> LayDanhSach(
            string tuKhoa, bool? trangThai)
        {
            var lst = DataProvider.Entities.ChuDeTinTucs
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                // ✅ Kiểm tra null MoTa trước khi Contains
                lst = lst.Where(x =>
                    x.TenChuDe.Contains(tuKhoa)
                 || (x.MoTa != null
                     && x.MoTa.Contains(tuKhoa)));

            if (trangThai.HasValue)
                lst = lst.Where(x =>
                    x.TrangThai == trangThai.Value);

            return lst.OrderByDescending(x => x.NgayTao)
                      .ToList();
        }

        public ChuDeTinTuc LayChiTiet(string maChuDe)
        {
            return DataProvider.Entities.ChuDeTinTucs
                       .Include("TinTucs")
                       .FirstOrDefault(x =>
                           x.MaChuDe == maChuDe);
        }

        public List<ChuDeTinTuc> LayDanhSachHoatDong()
        {
            return DataProvider.Entities.ChuDeTinTucs
                       .Where(x => x.TrangThai == true)
                       .OrderBy(x => x.TenChuDe)
                       .ToList();
        }

        public string SinhMa()
        {
            return "CD" + DateTime.Now
                              .ToString("yyyyMMddHHmmss");
        }

        public bool ThemMoi(ChuDeTinTuc obj)
        {
            if (obj == null) return false;

            // ✅ Dùng 1 instance
            var db = DataProvider.Entities;

            bool trung = db.ChuDeTinTucs
                            .Any(x => x.TenChuDe
                                       == obj.TenChuDe);
            if (trung) return false;

            obj.MaChuDe = SinhMa();
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            db.ChuDeTinTucs.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally
            {
                db.Configuration.ValidateOnSaveEnabled
                    = true;
            }
        }

        public bool CapNhat(ChuDeTinTuc obj)
        {
            var db = DataProvider.Entities;
            var objDb = db.ChuDeTinTucs
                           .FirstOrDefault(x =>
                               x.MaChuDe == obj.MaChuDe);
            if (objDb == null) return false;

            bool trung = db.ChuDeTinTucs
                            .Any(x =>
                                x.TenChuDe == obj.TenChuDe
                             && x.MaChuDe != obj.MaChuDe);
            if (trung) return false;

            objDb.TenChuDe = obj.TenChuDe;
            objDb.MoTa = obj.MoTa;
            objDb.TrangThai = obj.TrangThai;
            objDb.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally
            {
                db.Configuration.ValidateOnSaveEnabled
                    = true;
            }
        }

        public bool DoiTrangThai(string maChuDe)
        {
            var db = DataProvider.Entities;
            var obj = db.ChuDeTinTucs
                         .FirstOrDefault(x =>
                             x.MaChuDe == maChuDe);
            if (obj == null) return false;

            obj.TrangThai = !obj.TrangThai;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally
            {
                db.Configuration.ValidateOnSaveEnabled
                    = true;
            }
        }

        public bool Xoa(string maChuDe)
        {
            var db = DataProvider.Entities;
            var obj = db.ChuDeTinTucs
                         .FirstOrDefault(x =>
                             x.MaChuDe == maChuDe);
            if (obj == null) return false;

            // Không cho xóa nếu có bài viết
            bool coBaiViet = db.TinTucs
                               .Any(x => x.MaChuDe == maChuDe
                                      && x.TrangThai == true);
            if (coBaiViet) return false;

            db.ChuDeTinTucs.Remove(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally
            {
                db.Configuration.ValidateOnSaveEnabled = true;
            }
        }

        public int DemSoBaiViet(string maChuDe)
        {
            return DataProvider.Entities.TinTucs
                       .Count(x => x.MaChuDe == maChuDe
                                && x.TrangThai == true);
        }
    }
}