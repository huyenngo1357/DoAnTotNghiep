using HappyHouse.Models;
using PagedList;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class ChuTroThanhToanController : ChuTroBaseController
    {
        private readonly ThanhToanBusiness _bus = new ThanhToanBusiness();

        public ActionResult DanhSach(string trangThai = null, int page = 1)
        {
            var user = GetUserOnline();
            var lst = _bus.LayDanhSachChuTro(user.MaNguoiDung, trangThai);

            ViewBag.TrangThai = trangThai;
            ViewBag.SoChoXacNhan = _bus.DemChoXacNhan(user.MaNguoiDung);

            return View(lst.ToPagedList(page, 12));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhan(string maThanhToan)
        {
            var user = GetUserOnline();
            bool kq = _bus.XacNhan(maThanhToan, user.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xác nhận thanh toán."
                : "Thao tác thất bại!";

            return RedirectToAction("DanhSach");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoi(string maThanhToan, string lyDo)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["Error"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("DanhSach");
            }

            var user = GetUserOnline();
            bool kq = _bus.TuChoi(maThanhToan, lyDo, user.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã từ chối biên lai."
                : "Thao tác thất bại!";

            return RedirectToAction("DanhSach");
        }

        [HttpGet]
        public ActionResult CaiDatQR()
        {
            var user = GetUserOnline();
            var obj = DataProvider.Entities.NguoiDungs
                           .FirstOrDefault(x => x.MaNguoiDung
                                               == user.MaNguoiDung);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CaiDatQR(string soTaiKhoan,
                                      string tenNganHang,
                                      string tenTaiKhoan,
                                      HttpPostedFileBase anhQR)
        {
            var user = GetUserOnline();
            bool kq = _bus.CapNhatQR(user.MaNguoiDung,
                                       soTaiKhoan, tenNganHang,
                                       tenTaiKhoan, anhQR);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật thông tin QR thành công!"
                : "Cập nhật thất bại!";

            return RedirectToAction("CaiDatQR");
        }
    }
}