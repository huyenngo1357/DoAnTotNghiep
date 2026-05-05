using HappyHouse.Models;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class HopDongController : Controller
    {
        private NguoiDung KiemTraDangNhap()
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null || user.MaVaiTro != "KHACHHANG")
                return null;
            return user;
        }

        public ActionResult DanhSach()
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new { returnUrl = "/HopDong/DanhSach" });

            var lst = DataProvider.Entities.HopDongs
                          .Include("PhongTro")
                          .Include("PhongTro.ToaNha")
                          .Include("PhongTro.HinhAnhPhongs")
                          .Where(x => x.MaKhachHang == user.MaNguoiDung)
                          .OrderByDescending(x => x.NgayTao)
                          .ToList();

            // Dùng MaNguoiDung thay vì MaKhachHang
            var dsPhongDaDG = DataProvider.Entities.DanhGiaPhongs
                                  .Where(x => x.MaKhachHang == user.MaNguoiDung)
                                  .Select(x => x.MaPhong)
                                  .ToList();

            ViewBag.DsPhongDaDanhGia = dsPhongDaDG;
            return View(lst);
        }

        public ActionResult ChiTiet(string maHopDong)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var hopDong = DataProvider.Entities.HopDongs
                              .Include("PhongTro")
                              .Include("PhongTro.ToaNha")
                              .Include("PhongTro.ToaNha.NguoiDung")
                              .Include("PhongTro.HinhAnhPhongs")
                              .Include("PhongTro.TienIches")
                              .Include("HopDong_DichVu")
                              .Include("HopDong_DichVu.GiaDichVu")
                              .Include("HopDong_DichVu.GiaDichVu.TienIch")
                              .FirstOrDefault(x => x.MaHopDong == maHopDong
                                                && x.MaKhachHang
                                                   == user.MaNguoiDung);

            if (hopDong == null) return HttpNotFound();

            // Dùng MaNguoiDung thay vì MaKhachHang
            bool daDanhGia = DataProvider.Entities.DanhGiaPhongs
                                 .Any(x => x.MaPhong == hopDong.MaPhong
                                        && x.MaKhachHang == user.MaNguoiDung);

            ViewBag.DaDanhGia = daDanhGia;
            return View(hopDong);
        }
    }
}