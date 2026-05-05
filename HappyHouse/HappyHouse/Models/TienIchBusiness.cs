using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyHouse.Models
{
    public class TienIchBusiness
    {
        public TienIch LayChiTiet(string maTienIch)
        {
            return DataProvider.Entities.TienIches
                       .FirstOrDefault(x => x.MaTienIch == maTienIch);
        }

        public string SinhMa()
        {
            return "TI" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public List<TienIch> LayDanhSach(string tuKhoa = null,
                                  string coTinhTien = null,
                                  string trangThai = null)
        {
            var lst = DataProvider.Entities.TienIches
                          .Include("PhongTroes")      // ← thêm
                          .Include("GiaDichVus")      // ← thêm
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.TenTienIch.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(coTinhTien))
            {
                bool val = coTinhTien == "1";
                lst = lst.Where(x => x.CoTinhTien == val);
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                bool val = trangThai == "1";
                lst = lst.Where(x => x.TrangThai == val);
            }

            return lst.OrderBy(x => x.CoTinhTien)
                      .ThenBy(x => x.TenTienIch)
                      .ToList();
        }

        // Lấy tiện ích dùng cho hiển thị phòng (CoTinhTien = 0)
        public List<TienIch> LayTienIchHienThi()
        {
            return DataProvider.Entities.TienIches
                       .Where(x => x.CoTinhTien == false
                                && x.TrangThai == true)
                       .OrderBy(x => x.TenTienIch)
                       .ToList();
        }

        // Lấy tiện ích dùng cho dịch vụ tính tiền (CoTinhTien = 1)
        public List<TienIch> LayTienIchTinhTien()
        {
            return DataProvider.Entities.TienIches
                       .Where(x => x.CoTinhTien == true
                                && x.TrangThai == true)
                       .OrderBy(x => x.TenTienIch)
                       .ToList();
        }

        public bool ThemMoi(TienIch obj)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            bool trung = db.TienIches
                            .Any(x => x.TenTienIch == obj.TenTienIch);
            if (trung) return false;

            obj.MaTienIch = SinhMa();
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            db.TienIches.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool CapNhat(TienIch obj)
        {
            var db = DataProvider.Entities;
            var objDb = db.TienIches
                           .FirstOrDefault(x => x.MaTienIch == obj.MaTienIch);
            if (objDb == null) return false;

            bool trung = db.TienIches
                            .Any(x => x.TenTienIch == obj.TenTienIch
                                   && x.MaTienIch != obj.MaTienIch);
            if (trung) return false;

            objDb.TenTienIch = obj.TenTienIch;
            objDb.BieuTuong = obj.BieuTuong;
            objDb.CoTinhTien = obj.CoTinhTien;
            objDb.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool DoiTrangThai(string maTienIch)
        {
            var db = DataProvider.Entities;
            var obj = db.TienIches
                         .FirstOrDefault(x => x.MaTienIch == maTienIch);
            if (obj == null) return false;

            obj.TrangThai = !obj.TrangThai;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool Xoa(string maTienIch)
        {
            var db = DataProvider.Entities;
            var obj = db.TienIches
                         .FirstOrDefault(x => x.MaTienIch == maTienIch);
            if (obj == null) return false;

            // Không cho xóa nếu đang dùng ở GiaDichVu
            bool dangDungDichVu = db.GiaDichVus
                                    .Any(g => g.MaTienIch == maTienIch
                                           && g.TrangThai == true);
            if (dangDungDichVu) return false;

            // Không cho xóa nếu đang gắn với phòng
            // → Kiểm tra qua navigation property của PhongTro
            bool dangDungPhong = db.PhongTroes
                                    .Any(p => p.TienIches
                                               .Any(t => t.MaTienIch == maTienIch));
            if (dangDungPhong) return false;

            db.TienIches.Remove(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public int DemSoPhongDung(string maTienIch)
        {
            return DataProvider.Entities.PhongTroes
                       .Count(p => p.TienIches
                                    .Any(t => t.MaTienIch == maTienIch));
        }

        public int DemSoDichVuDung(string maTienIch)
        {
            return DataProvider.Entities.GiaDichVus
                       .Count(x => x.MaTienIch == maTienIch
                                && x.TrangThai == true);
        }
    }
}