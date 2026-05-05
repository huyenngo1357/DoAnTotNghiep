using HappyHouse.Models;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ThanhToanController : Controller
    {
        private readonly ThanhToanBusiness _bus = new ThanhToanBusiness();

        private NguoiDung KiemTraKhachHang()
        {
            var user = Session["UserOnline"] as NguoiDung;
            if (user == null || user.MaVaiTro != "KHACHHANG")
                return null;
            return user;
        }

        public ActionResult DanhSach(string trangThai = null)
        {
            var user = KiemTraKhachHang();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new { returnUrl = "/ThanhToan/DanhSach" });

            var lst = DataProvider.Entities.HoaDons
                          .Include("HopDong")
                          .Include("HopDong.PhongTro")
                          .Include("HopDong.PhongTro.ToaNha")
                          .Include("ThanhToans")
                          .Where(x => x.HopDong.MaKhachHang == user.MaNguoiDung
                                   && x.TrangThai == true)
                          .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai))
                lst = lst.Where(x => x.TrangThaiHoaDon == trangThai);

            // Badge: đếm cả ChuaThanhToan + QuaHan (chưa upload biên lai)
            ViewBag.SoChuaThanhToan = DataProvider.Entities.HoaDons
                .Count(x => x.HopDong.MaKhachHang == user.MaNguoiDung
                         && x.TrangThai == true
                         && (x.TrangThaiHoaDon == "ChuaThanhToan"
                          || x.TrangThaiHoaDon == "QuaHan"));

            ViewBag.TrangThai = trangThai;

            return View(lst.OrderByDescending(x => x.NgayTao).ToList());
        }

        [HttpGet]
        public ActionResult ThanhToan(string maHoaDon)
        {
            var user = KiemTraKhachHang();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan",
                    new
                    {
                        returnUrl = "/ThanhToan/ThanhToan?maHoaDon="
                                      + maHoaDon
                    });

            var hoaDon = DataProvider.Entities.HoaDons
                             .Include("HopDong")
                             .Include("HopDong.PhongTro")
                             .Include("HopDong.PhongTro.ToaNha")
                             .Include("ThanhToans")
                             .FirstOrDefault(x => x.MaHoaDon == maHoaDon
                                               && x.HopDong.MaKhachHang
                                                  == user.MaNguoiDung
                                               && x.TrangThai == true);

            if (hoaDon == null) return HttpNotFound();

            if (hoaDon.TrangThaiHoaDon == "DaThanhToan")
            {
                TempData["Error"] = "Hóa đơn này đã được thanh toán!";
                return RedirectToAction("DanhSach");
            }

            // QR của chủ trọ
            var chuTro = DataProvider.Entities.NguoiDungs
                             .FirstOrDefault(x => x.MaNguoiDung
                                               == hoaDon.HopDong.MaChuTro);
            ViewBag.ChuTro = chuTro;

            // Biên lai đang chờ xác nhận
            var bienLaiCho = hoaDon.ThanhToans
                                   .Where(x => x.TrangThaiXacNhan == "ChoXacNhan"
                                            && x.TrangThai == true)
                                   .OrderByDescending(x => x.NgayTao)
                                   .FirstOrDefault();
            ViewBag.BienLaiCho = bienLaiCho;

            // Biên lai bị từ chối gần nhất
            var bienLaiTuChoi = hoaDon.ThanhToans
                                      .Where(x => x.TrangThaiXacNhan == "TuChoi"
                                               && x.TrangThai == true)
                                      .OrderByDescending(x => x.NgayTao)
                                      .FirstOrDefault();
            ViewBag.BienLaiTuChoi = bienLaiTuChoi;

            return View(hoaDon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadBienLai(string maHoaDon,
                                           string hinhThuc,
                                           string maGiaoDich,
                                           string ghiChu,
                                           HttpPostedFileBase bienLai)
        {
            var user = KiemTraKhachHang();
            if (user == null)
                return RedirectToAction("DangNhap", "TaiKhoan");

            if (bienLai == null || bienLai.ContentLength == 0)
            {
                TempData["Error"] = "Vui lòng chọn ảnh biên lai!";
                return RedirectToAction("ThanhToan", new { maHoaDon });
            }

            string ext = System.IO.Path
                .GetExtension(bienLai.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg"
             && ext != ".png" && ext != ".pdf")
            {
                TempData["Error"] = "Chỉ chấp nhận JPG, PNG hoặc PDF!";
                return RedirectToAction("ThanhToan", new { maHoaDon });
            }

            bool kq = _bus.NopBienLai(
                maHoaDon, user.MaNguoiDung,
                hinhThuc, maGiaoDich, ghiChu, bienLai);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã gửi biên lai! Vui lòng chờ chủ trọ xác nhận."
                : "Bạn đã có biên lai đang chờ xác nhận. Vui lòng chờ!";

            return RedirectToAction("ThanhToan", new { maHoaDon });
        }
    }
}