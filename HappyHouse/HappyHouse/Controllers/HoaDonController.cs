using HappyHouse.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class HoaDonController : Controller
    {
        private NguoiDung KiemTraDangNhap()
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null || user.MaVaiTro != "KHACHHANG")
                return null;
            return user;
        }

        // ── DANH SÁCH HÓA ĐƠN ───────────────────────────────────────
        public ActionResult DanhSach(string trangThai = null)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new { returnUrl = "/HoaDon/DanhSach" });

            var lst = DataProvider.Entities.HoaDons
                          .Include("HopDong")
                          .Include("HopDong.PhongTro")
                          .Include("HopDong.PhongTro.ToaNha")
                          .Where(x => x.HopDong.MaKhachHang == user.MaNguoiDung
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                lst = lst.Where(x => x.TrangThaiHoaDon == trangThai);

            ViewBag.TrangThai = trangThai;
            ViewBag.TongChuaThanhToan = DataProvider.Entities.HoaDons
                .Count(x => x.HopDong.MaKhachHang == user.MaNguoiDung
                         && x.TrangThai == true
                         && x.TrangThaiHoaDon == "ChuaThanhToan");

            return View(lst.OrderByDescending(x => x.NgayTao).ToList());
        }

        // ── CHI TIẾT HÓA ĐƠN ────────────────────────────────────────
        public ActionResult ChiTiet(string maHoaDon)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var hoaDon = DataProvider.Entities.HoaDons
                             .Include("HopDong")
                             .Include("HopDong.PhongTro")
                             .Include("HopDong.PhongTro.ToaNha")
                             .Include("HopDong.PhongTro.ToaNha.NguoiDung")
                             .Include("ThanhToans")
                             .FirstOrDefault(x => x.MaHoaDon == maHoaDon
                                               && x.HopDong.MaKhachHang == user.MaNguoiDung
                                               && x.TrangThai == true);

            if (hoaDon == null) return HttpNotFound();

            return View(hoaDon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadBienLai(string maHoaDon,
                                   HttpPostedFileBase bienLai)
        {
            var user = KiemTraDangNhap();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            var hoaDon = DataProvider.Entities.HoaDons
                             .Include("HopDong")
                             .FirstOrDefault(x => x.MaHoaDon == maHoaDon
                                               && x.HopDong.MaKhachHang
                                                  == user.MaNguoiDung
                                               && x.TrangThai == true);

            if (hoaDon == null) return HttpNotFound();

            if (hoaDon.TrangThaiHoaDon != "ChuaThanhToan"
             && hoaDon.TrangThaiHoaDon != "QuaHan")
            {
                TempData["Error"] = "Hóa đơn này không ở trạng thái "
                                  + "chờ thanh toán!";
                return RedirectToAction("ChiTiet", new { maHoaDon });
            }

            if (bienLai == null || bienLai.ContentLength == 0)
            {
                TempData["Error"] = "Vui lòng chọn file biên lai!";
                return RedirectToAction("ChiTiet", new { maHoaDon });
            }

            string ext = System.IO.Path
                .GetExtension(bienLai.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg"
             && ext != ".png" && ext != ".pdf")
            {
                TempData["Error"] = "Chỉ chấp nhận JPG, PNG hoặc PDF!";
                return RedirectToAction("ChiTiet", new { maHoaDon });
            }

            // Lưu file
            string folder = Server.MapPath(
                "~/Content/images/bienlai/");
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            string tenFile = "BL_" + maHoaDon + "_"
                           + DateTime.Now.ToString("yyyyMMddHHmmss")
                           + ext;
            bienLai.SaveAs(
                System.IO.Path.Combine(folder, tenFile));

            // Tạo ThanhToan
            var thanhToan = new ThanhToan
            {
                MaThanhToan = "TT" + DateTime.Now
                                       .ToString("yyyyMMddHHmmss"),
                MaHoaDon = maHoaDon,
                MaKhachHang = user.MaNguoiDung,
                SoTien = hoaDon.TongTien,
                HinhThuc = "ChuyenKhoan",
                AnhBienLai = tenFile,
                TrangThaiXacNhan = "ChoXacNhan",
                TrangThai = true,
                NgayThanhToan = DateTime.Now,
                NgayTao = DateTime.Now,
            };

            DataProvider.Entities.ThanhToans.Add(thanhToan);

            // ✅ Giờ đã sửa DB nên set được "ChoDuyet"
            hoaDon.TrangThaiHoaDon = "ChoDuyet";
            hoaDon.NgayCapNhat = DateTime.Now;

            DataProvider.Entities.Configuration
                .ValidateOnSaveEnabled = false;
            try
            {
                DataProvider.Entities.SaveChanges();
                TempData["Success"] =
                    "Đã gửi biên lai thành công! "
                  + "Vui lòng chờ chủ trọ xác nhận.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi lưu: " + ex.Message;
            }
            finally
            {
                DataProvider.Entities.Configuration
                    .ValidateOnSaveEnabled = true;
            }

            return RedirectToAction("ChiTiet", new { maHoaDon });
        }
    }
}