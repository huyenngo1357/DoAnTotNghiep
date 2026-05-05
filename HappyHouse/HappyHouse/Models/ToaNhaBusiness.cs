using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class ToaNhaBusiness
    {
        // ════════════════════════════════════════════════════════════
        //  DÙNG CHUNG
        // ════════════════════════════════════════════════════════════

        public ToaNha LayChiTiet(string maToaNha)
        {
            return DataProvider.Entities.ToaNhas
                       .Include("NguoiDung")       // ChuTro
                       .Include("NguoiDung1")      // NguoiDuyet
                       .Include("HinhAnhToaNhas")
                       .Include("PhongTroes")
                       .FirstOrDefault(x => x.MaToaNha == maToaNha);
        }

        public string SinhMa()
        {
            return "TN" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        // ════════════════════════════════════════════════════════════
        //  ADMIN
        // ════════════════════════════════════════════════════════════

        public List<ToaNha> LayDanhSachAdmin(string tuKhoa, string trangThaiDuyet)
        {
            var lst = DataProvider.Entities.ToaNhas
                          .Include("NguoiDung")
                          .Include("HinhAnhToaNhas")
                          .Where(x => x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.TenToaNha.Contains(tuKhoa)
                                  || x.DiaChi.Contains(tuKhoa)
                                  || x.TinhThanh.Contains(tuKhoa)
                                  || x.NguoiDung.HoTen.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(trangThaiDuyet))
                lst = lst.Where(x => x.TrangThaiDuyet == trangThaiDuyet);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        // Đếm theo trạng thái để hiển thị badge trên menu
        public int DemChoduyet()
        {
            return DataProvider.Entities.ToaNhas
                       .Count(x => x.TrangThaiDuyet == "ChoDuyet"
                                && x.TrangThai == true);
        }

        /// <summary>Duyệt tòa nhà — gửi ThongBao cho chủ trọ</summary>
        public bool DuyetToaNha(string maToaNha, string maNguoiDuyet)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha);
            if (obj == null) return false;

            obj.TrangThaiDuyet = "DaDuyet";
            obj.MaNguoiDuyet = maNguoiDuyet;
            obj.NgayDuyet = DateTime.Now;
            obj.LyDoTuChoi = null;
            obj.NgayCapNhat = DateTime.Now;

            // Gửi thông báo cho chủ trọ
            GuiThongBao(db, obj.MaChuTro,
                title: "Tòa nhà \"" + obj.TenToaNha + "\" đã được duyệt",
                noiDung: "Tòa nhà của bạn đã được admin duyệt. Bạn có thể bắt đầu thêm phòng.",
                loai: "HeThong",
                duongDan: "/ChuTroToaNha/DanhSach");

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        /// <summary>Từ chối tòa nhà — ghi lý do + gửi ThongBao</summary>
        public bool TuChoiToaNha(string maToaNha, string maNguoiDuyet, string lyDo)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha);
            if (obj == null) return false;

            obj.TrangThaiDuyet = "TuChoi";
            obj.MaNguoiDuyet = maNguoiDuyet;
            obj.NgayDuyet = DateTime.Now;
            obj.LyDoTuChoi = lyDo;
            obj.NgayCapNhat = DateTime.Now;

            GuiThongBao(db, obj.MaChuTro,
                title: "Tòa nhà \"" + obj.TenToaNha + "\" bị từ chối",
                noiDung: "Lý do: " + lyDo + ". Vui lòng chỉnh sửa và gửi lại.",
                loai: "HeThong",
                duongDan: "/ChuTroToaNha/SuaThongTin?maToaNha=" + maToaNha);

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        /// <summary>Tạm ngưng tòa nhà đang DaDuyet</summary>
        public bool TamNgungToaNha(string maToaNha, string maNguoiDuyet, string lyDo)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha);
            if (obj == null) return false;
            if (obj.TrangThaiDuyet != "DaDuyet") return false;

            obj.TrangThaiDuyet = "TamNgung";
            obj.MaNguoiDuyet = maNguoiDuyet;
            obj.NgayDuyet = DateTime.Now;
            obj.LyDoTuChoi = lyDo;
            obj.NgayCapNhat = DateTime.Now;

            GuiThongBao(db, obj.MaChuTro,
                title: "Tòa nhà \"" + obj.TenToaNha + "\" bị tạm ngưng",
                noiDung: "Lý do: " + lyDo,
                loai: "HeThong",
                duongDan: "/ChuTroToaNha/DanhSach");

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        /// <summary>Mở lại tòa nhà đang TamNgung</summary>
        public bool MoLaiToaNha(string maToaNha, string maNguoiDuyet)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha);
            if (obj == null) return false;
            if (obj.TrangThaiDuyet != "TamNgung") return false;

            obj.TrangThaiDuyet = "DaDuyet";
            obj.MaNguoiDuyet = maNguoiDuyet;
            obj.NgayDuyet = DateTime.Now;
            obj.LyDoTuChoi = null;
            obj.NgayCapNhat = DateTime.Now;

            GuiThongBao(db, obj.MaChuTro,
                title: "Tòa nhà \"" + obj.TenToaNha + "\" đã được mở lại",
                noiDung: "Tòa nhà của bạn đã được admin mở lại hoạt động bình thường.",
                loai: "HeThong",
                duongDan: "/ChuTroToaNha/DanhSach");

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool XoaAdmin(string maToaNha)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha);
            if (obj == null) return false;

            obj.TrangThai = false;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        // ════════════════════════════════════════════════════════════
        //  CHỦ TRỌ
        // ════════════════════════════════════════════════════════════

        public List<ToaNha> LayDanhSachChuTro(string maChuTro,
                                               string tuKhoa,
                                               string trangThaiDuyet)
        {
            var lst = DataProvider.Entities.ToaNhas
                          .Include("HinhAnhToaNhas")
                          .Include("NguoiDung1")   // NguoiDuyet
                          .Where(x => x.MaChuTro == maChuTro
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(tuKhoa))
                lst = lst.Where(x => x.TenToaNha.Contains(tuKhoa)
                                  || x.DiaChi.Contains(tuKhoa));

            if (!string.IsNullOrEmpty(trangThaiDuyet))
                lst = lst.Where(x => x.TrangThaiDuyet == trangThaiDuyet);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }

        public bool ThemMoi(ToaNha obj, List<HttpPostedFileBase> dsHinhAnh)
        {
            if (obj == null) return false;

            var db = DataProvider.Entities;

            obj.TrangThaiDuyet = "ChoDuyet";
            obj.TrangThai = true;
            obj.NgayTao = DateTime.Now;

            db.ToaNhas.Add(obj);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            if (dsHinhAnh != null && dsHinhAnh.Count > 0)
                LuuHinhAnh(obj.MaToaNha, dsHinhAnh);

            return true;
        }

        public bool CapNhat(ToaNha obj, List<HttpPostedFileBase> dsHinhAnh)
        {
            var db = DataProvider.Entities;
            var objDb = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == obj.MaToaNha);
            if (objDb == null) return false;

            objDb.TenToaNha = obj.TenToaNha;
            objDb.DiaChi = obj.DiaChi;
            objDb.PhuongXa = obj.PhuongXa;
            objDb.TinhThanh = obj.TinhThanh;
            objDb.MoTa = obj.MoTa;
            objDb.TrangThaiDuyet = "ChoDuyet";  // gửi duyệt lại sau khi sửa
            objDb.LyDoTuChoi = null;
            objDb.MaNguoiDuyet = null;
            objDb.NgayDuyet = null;
            objDb.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            bool kq;
            try { kq = db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            if (kq && dsHinhAnh != null && dsHinhAnh.Count > 0)
                LuuHinhAnh(obj.MaToaNha, dsHinhAnh);

            return kq;
        }

        // Chủ trọ gửi yêu cầu xóa → set TrangThaiDuyet = YeuCauXoa
        public bool GuiYeuCauXoa(string maToaNha, string maChuTro)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas
                         .Include("NguoiDung")
                         .FirstOrDefault(x => x.MaToaNha == maToaNha
                                           && x.MaChuTro == maChuTro);
            if (obj == null) return false;

            bool dangCoHopDong = db.HopDongs
                                    .Any(h => h.PhongTro.MaToaNha == maToaNha
                                           && h.TrangThaiHopDong == "DangThue");
            if (dangCoHopDong) return false;

            string tenChuTro = obj.NguoiDung?.HoTen ?? "";
            obj.TrangThaiDuyet = "YeuCauXoa";
            obj.LyDoTuChoi = "Chủ trọ yêu cầu xóa tòa nhà.";
            obj.NgayCapNhat = DateTime.Now;

            // Thông báo tất cả admin
            var dsAdmin = db.NguoiDungs
                            .Where(x => x.MaVaiTro == "CHUTHAU"
                                     && x.TrangThai == true)
                            .ToList();

            foreach (var admin in dsAdmin)
            {
                GuiThongBao(db, admin.MaNguoiDung,
                    "Yêu cầu xóa tòa nhà",
                    tenChuTro + " yêu cầu xóa tòa nhà \"" + obj.TenToaNha + "\".",
                    "HeThong",
                    "/AdminToaNha/DanhSach");
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        // Chủ trọ hủy yêu cầu xóa → về ChoDuyet
        public bool HuyYeuCauXoa(string maToaNha, string maChuTro)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas
                         .FirstOrDefault(x => x.MaToaNha == maToaNha
                                           && x.MaChuTro == maChuTro);
            if (obj == null) return false;
            if (obj.TrangThaiDuyet != "YeuCauXoa") return false;

            obj.TrangThaiDuyet = "ChoDuyet";
            obj.LyDoTuChoi = null;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public int DemYeuCauXoa()
        {
            return DataProvider.Entities.ToaNhas
                       .Count(x => x.TrangThaiDuyet == "YeuCauXoa"
                                && x.TrangThai == true);
        }

        /// <summary>Xóa mềm — không cho xóa khi còn hợp đồng đang thuê</summary>
        public bool Xoa(string maToaNha, string maChuTro)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha
                                                   && x.MaChuTro == maChuTro);
            if (obj == null) return false;

            bool dangCoHopDong = db.HopDongs
                                    .Any(h => h.PhongTro.MaToaNha == maToaNha
                                           && h.TrangThaiHopDong == "DangThue");
            if (dangCoHopDong) return false;

            obj.TrangThai = false;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool XoaHinhAnh(int maHinhAnh)
        {
            var db = DataProvider.Entities;
            var hinh = db.HinhAnhToaNhas.FirstOrDefault(x => x.MaHinhAnh == maHinhAnh);
            if (hinh == null) return false;

            string duongDan = HttpContext.Current.Server
                                  .MapPath("~/Content/images/toanhas/" + hinh.TenFile);
            if (File.Exists(duongDan)) File.Delete(duongDan);

            db.HinhAnhToaNhas.Remove(hinh);
            db.SaveChanges();
            return true;
        }

        // ════════════════════════════════════════════════════════════
        //  PRIVATE
        // ════════════════════════════════════════════════════════════

        private void GuiThongBao(HappyHouseEntities db,
                                  string maNguoiNhan,
                                  string title,
                                  string noiDung,
                                  string loai,
                                  string duongDan)
        {
            db.ThongBaos.Add(new ThongBao
            {
                MaNguoiDung = maNguoiNhan,
                TieuDe = title,
                NoiDung = noiDung,
                LoaiThongBao = loai,
                DuongDan = duongDan,
                DaDoc = false,
                NgayTao = DateTime.Now
            });
        }

        private void LuuHinhAnh(string maToaNha, List<HttpPostedFileBase> dsHinhAnh)
        {
            var db = DataProvider.Entities;

            string folder = HttpContext.Current.Server
                                .MapPath("~/Content/images/toanhas/");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            bool daCoDaiDien = db.HinhAnhToaNhas
                                  .Any(x => x.MaToaNha == maToaNha
                                         && x.LaDaiDien == true);
            bool laDaiDienDau = !daCoDaiDien;

            foreach (var file in dsHinhAnh)
            {
                if (file == null || file.ContentLength == 0) continue;
                if (!file.ContentType.StartsWith("image")) continue;

                string tenFile = Guid.NewGuid() + Path.GetExtension(file.FileName);
                file.SaveAs(Path.Combine(folder, tenFile));

                db.HinhAnhToaNhas.Add(new HinhAnhToaNha
                {
                    MaToaNha = maToaNha,
                    TenFile = tenFile,
                    LaDaiDien = laDaiDienDau,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                });
                laDaiDienDau = false;
            }

            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }
    }
}