using HappyHouse.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class DanhGiaController : Controller
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
                    new { returnUrl = "/DanhGia/DanhSach" });

            // FIX: "DaKetThuc" không tồn tại trong DB
            // CHECK constraint chỉ có: ChoKy, DangThue, HetHan, DaHuy
            // Cho phép đánh giá khi hợp đồng HetHan hoặc DaHuy
            var dsHopDong = DataProvider.Entities.HopDongs
                                .Include("PhongTro")
                                .Include("PhongTro.ToaNha")
                                .Include("PhongTro.HinhAnhPhongs")
                                .Where(x => x.MaKhachHang == user.MaNguoiDung
                                         && (x.TrangThaiHopDong == "HetHan"
                                          || x.TrangThaiHopDong == "DaHuy"))
                                .OrderByDescending(x => x.NgayKetThuc)
                                .ToList();

            var daMaDanhGia = DataProvider.Entities.DanhGiaPhongs
                                  .Where(x => x.MaKhachHang == user.MaNguoiDung)
                                  .Select(x => x.MaHopDong)
                                  .ToList();

            ViewBag.DaMaDanhGia = daMaDanhGia;
            return View(dsHopDong);
        }

        [HttpGet]
        public ActionResult TaoDanhGia(string maHopDong)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            // FIX: "DaKetThuc" → "HetHan" hoặc "DaHuy"
            var hopDong = DataProvider.Entities.HopDongs
                              .Include("PhongTro")
                              .Include("PhongTro.ToaNha")
                              .Include("PhongTro.HinhAnhPhongs")
                              .FirstOrDefault(x => x.MaHopDong == maHopDong
                                                && x.MaKhachHang == user.MaNguoiDung
                                                && (x.TrangThaiHopDong == "HetHan"
                                                 || x.TrangThaiHopDong == "DaHuy"));

            if (hopDong == null) return HttpNotFound();

            bool daDanhGia = DataProvider.Entities.DanhGiaPhongs
                                 .Any(x => x.MaHopDong == maHopDong
                                        && x.MaKhachHang == user.MaNguoiDung);
            if (daDanhGia)
            {
                TempData["Error"] = "Bạn đã đánh giá phòng này rồi!";
                return RedirectToAction("DanhSach");
            }

            return View(hopDong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TaoDanhGia(string maHopDong,
                                       string nhanXet,
                                       byte diemDanhGia)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            // FIX: "DaKetThuc" → "HetHan" hoặc "DaHuy"
            var hopDong = DataProvider.Entities.HopDongs
                              .FirstOrDefault(x => x.MaHopDong == maHopDong
                                                && x.MaKhachHang == user.MaNguoiDung
                                                && (x.TrangThaiHopDong == "HetHan"
                                                 || x.TrangThaiHopDong == "DaHuy"));
            if (hopDong == null) return HttpNotFound();

            bool daDanhGia = DataProvider.Entities.DanhGiaPhongs
                                 .Any(x => x.MaHopDong == maHopDong
                                        && x.MaKhachHang == user.MaNguoiDung);
            if (daDanhGia)
            {
                TempData["Error"] = "Bạn đã đánh giá phòng này rồi!";
                return RedirectToAction("DanhSach");
            }

            if (diemDanhGia < 1 || diemDanhGia > 5)
            {
                TempData["Error"] = "Điểm đánh giá phải từ 1 đến 5!";
                return RedirectToAction("TaoDanhGia", new { maHopDong });
            }

            if (string.IsNullOrWhiteSpace(nhanXet))
            {
                TempData["Error"] = "Vui lòng nhập nhận xét!";
                return RedirectToAction("TaoDanhGia", new { maHopDong });
            }

            var danhGia = new DanhGiaPhong
            {
                MaDanhGia = "DG" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                MaHopDong = maHopDong,
                MaKhachHang = user.MaNguoiDung,
                MaPhong = hopDong.MaPhong,
                NhanXet = nhanXet.Trim(),
                DiemDanhGia = diemDanhGia,
                TrangThai = true,
                NgayTao = DateTime.Now,
            };

            var db = DataProvider.Entities;
            db.DanhGiaPhongs.Add(danhGia);
            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            TempData["Success"] = "Cảm ơn bạn đã đánh giá! Đánh giá đã được ghi nhận.";
            return RedirectToAction("DanhSach");
        }
    }
}