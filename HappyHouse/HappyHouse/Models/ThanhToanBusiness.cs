using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class ThanhToanBusiness
    {
        public string SinhMa()
        {
            return "TT" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public bool NopBienLai(string maHoaDon,
                        string maNguoiDung,
                        string hinhThuc,
                        string maGiaoDich,
                        string ghiChu,
                        HttpPostedFileBase bienLai)
        {
            var db = DataProvider.Entities;

            bool dangCho = db.ThanhToans
                              .Any(x => x.MaHoaDon == maHoaDon
                                     && x.TrangThaiXacNhan == "ChoXacNhan"
                                     && x.TrangThai == true);
            if (dangCho) return false;

            var hoaDon = db.HoaDons
                            .FirstOrDefault(x => x.MaHoaDon == maHoaDon);
            if (hoaDon == null) return false;

            string tenFile = null;
            if (bienLai != null && bienLai.ContentLength > 0)
                tenFile = LuuBienLai(bienLai, maHoaDon);

            var tt = new ThanhToan
            {
                MaThanhToan = SinhMa(),
                MaHoaDon = maHoaDon,
                MaKhachHang = maNguoiDung,
                SoTien = hoaDon.TongTien,
                HinhThuc = hinhThuc ?? "ChuyenKhoan",
                MaGiaoDich = maGiaoDich,
                AnhBienLai = tenFile,
                GhiChu = ghiChu,
                TrangThaiXacNhan = "ChoXacNhan",
                TrangThai = true,
                NgayThanhToan = DateTime.Now,
                NgayTao = DateTime.Now
            };

            db.ThanhToans.Add(tt);

            hoaDon.TrangThaiHoaDon = "ChoDuyet";
            hoaDon.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool XacNhan(string maThanhToan, string nguoiXacNhanId)
        {
            var db = DataProvider.Entities;
            var tt = db.ThanhToans
                        .FirstOrDefault(x => x.MaThanhToan == maThanhToan);
            if (tt == null || tt.TrangThaiXacNhan != "ChoXacNhan")
                return false;

            tt.TrangThaiXacNhan = "DaXacNhan";
            tt.NguoiXacNhanId = nguoiXacNhanId;
            tt.NgayCapNhat = DateTime.Now;

            var hoaDon = db.HoaDons
                            .FirstOrDefault(x => x.MaHoaDon == tt.MaHoaDon);
            if (hoaDon != null)
            {
                hoaDon.TrangThaiHoaDon = "DaThanhToan";
                hoaDon.NgayThanhToan = DateTime.Now;
                hoaDon.NgayCapNhat = DateTime.Now;
            }

            GuiThongBao(db, tt.MaKhachHang,
                "Thanh toán đã được xác nhận",
                "Chủ trọ đã xác nhận thanh toán hóa đơn của bạn.",
                "ThanhToan",
                "/HoaDon/DanhSach");

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public bool TuChoi(string maThanhToan,
                    string lyDo,
                    string nguoiXacNhanId)
        {
            var db = DataProvider.Entities;
            var tt = db.ThanhToans
                        .FirstOrDefault(x => x.MaThanhToan == maThanhToan);
            if (tt == null || tt.TrangThaiXacNhan != "ChoXacNhan")
                return false;

            tt.TrangThaiXacNhan = "TuChoi";
            tt.LyDoTuChoi = lyDo;
            tt.NguoiXacNhanId = nguoiXacNhanId;
            tt.NgayCapNhat = DateTime.Now;

            var hoaDon = db.HoaDons
                            .FirstOrDefault(x => x.MaHoaDon == tt.MaHoaDon);
            if (hoaDon != null)
            {
                hoaDon.TrangThaiHoaDon = "ChuaThanhToan";
                hoaDon.NgayCapNhat = DateTime.Now;
            }

            GuiThongBao(db, tt.MaKhachHang,
                "Biên lai thanh toán bị từ chối",
                "Lý do: " + lyDo + ". Vui lòng kiểm tra và gửi lại.",
                "ThanhToan",
                "/ThanhToan/DanhSach");

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        public List<ThanhToan> LayDanhSachChoXacNhan(string maChuTro)
        {
            return DataProvider.Entities.ThanhToans
                       .Include("HoaDon")
                       .Include("HoaDon.HopDong")
                       .Include("HoaDon.HopDong.NguoiDung1")
                       .Include("HoaDon.HopDong.PhongTro")
                       .Include("HoaDon.HopDong.PhongTro.ToaNha")
                       .Where(x => x.HoaDon.HopDong.MaChuTro == maChuTro
                                && x.TrangThaiXacNhan == "ChoXacNhan"
                                && x.TrangThai == true)
                       .OrderByDescending(x => x.NgayTao)
                       .ToList();
        }

        public List<ThanhToan> LayDanhSachChuTro(string maChuTro,
                                                  string trangThai = null)
        {
            var lst = DataProvider.Entities.ThanhToans
                          .Include("HoaDon")
                          .Include("HoaDon.HopDong")
                          .Include("HoaDon.HopDong.NguoiDung1")
                          .Include("HoaDon.HopDong.PhongTro")
                          .Include("HoaDon.HopDong.PhongTro.ToaNha")
                          .Where(x => x.HoaDon.HopDong.MaChuTro == maChuTro
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                lst = lst.Where(x => x.TrangThaiXacNhan == trangThai);

            return lst.OrderByDescending(x => x.NgayTao).ToList();
        }
        public int DemChoXacNhan(string maChuTro)
        {
            return DataProvider.Entities.ThanhToans
                       .Count(x => x.HoaDon.HopDong.MaChuTro == maChuTro
                                && x.TrangThaiXacNhan == "ChoXacNhan"
                                && x.TrangThai == true);
        }

        public bool CapNhatQR(string maChuTro,
                               string soTaiKhoan,
                               string tenNganHang,
                               string tenTaiKhoan,
                               HttpPostedFileBase anhQR)
        {
            var db = DataProvider.Entities;
            var obj = db.NguoiDungs
                         .FirstOrDefault(x => x.MaNguoiDung == maChuTro);
            if (obj == null) return false;

            obj.SoTaiKhoan = soTaiKhoan;
            obj.TenNganHang = tenNganHang;
            obj.TenTaiKhoan = tenTaiKhoan;

            if (anhQR != null && anhQR.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(obj.AnhQR))
                    XoaAnhVatLy(obj.AnhQR, "qr");
                obj.AnhQR = LuuAnh(anhQR, "qr");
            }

            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { return db.SaveChanges() > 0; }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }
        }

        private string LuuBienLai(HttpPostedFileBase file, string maHoaDon)
        {
            string folder = HttpContext.Current.Server
                                .MapPath("~/Content/images/bienlai/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string ext = Path.GetExtension(file.FileName).ToLower();
            string tenFile = "BL_" + maHoaDon + "_"
                           + DateTime.Now.ToString("yyyyMMddHHmmss") + ext;
            file.SaveAs(Path.Combine(folder, tenFile));
            return tenFile;
        }

        private string LuuAnh(HttpPostedFileBase file, string subFolder)
        {
            string folder = HttpContext.Current.Server
                                .MapPath("~/Content/images/" + subFolder + "/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string tenFile = Guid.NewGuid()
                           + Path.GetExtension(file.FileName);
            file.SaveAs(Path.Combine(folder, tenFile));
            return tenFile;
        }

        private void XoaAnhVatLy(string tenFile, string subFolder)
        {
            string path = HttpContext.Current.Server
                              .MapPath("~/Content/images/" + subFolder
                                       + "/" + tenFile);
            if (File.Exists(path)) File.Delete(path);
        }

        private void GuiThongBao(HappyHouseEntities db,
                                   string maNguoiNhan,
                                   string tieuDe,
                                   string noiDung,
                                   string loai,
                                   string duongDan)
        {
            db.ThongBaos.Add(new ThongBao
            {
                MaNguoiDung = maNguoiNhan,
                TieuDe = tieuDe,
                NoiDung = noiDung,
                LoaiThongBao = loai,
                DuongDan = duongDan,
                DaDoc = false,
                NgayTao = DateTime.Now
            });
        }
    }
}