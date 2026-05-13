using HappyHouse.Models;
using PagedList;
using System;
using System.Linq;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class AdminToaNhaController : AdminBaseController
    {
        private readonly ToaNhaBusiness _bus = new ToaNhaBusiness();

        private void LoadDropdown(string trangThaiDuyet = null)
        {
            var lst = new[]
            {
                new { Value = "",            Text = "--- Tất cả ---" },
                new { Value = "ChoDuyet",    Text = "Chờ duyệt"     },
                new { Value = "DaDuyet",     Text = "Đã duyệt"      },
                new { Value = "TuChoi",      Text = "Từ chối"        },
                new { Value = "TamNgung",    Text = "Tạm ngưng"     },
                new { Value = "YeuCauXoa",   Text = "Yêu cầu xóa"  },
            };
            ViewBag.DsTrangThai = new SelectList(lst, "Value", "Text", trangThaiDuyet);
        }

        // DANH SÁCH

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                     string tuKhoa = null,
                                     string trangThaiDuyet = null)
        {
            return HienThiDanhSach(page, tuKhoa, trangThaiDuyet);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                     string trangThaiDuyet,
                                     int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, trangThaiDuyet);
        }

        private ActionResult HienThiDanhSach(int page,
                                              string tuKhoa,
                                              string trangThaiDuyet)
        {
            var lst = _bus.LayDanhSachAdmin(tuKhoa, trangThaiDuyet);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThaiDuyet = trangThaiDuyet;
            ViewBag.SoChoduyet = _bus.DemChoduyet();
            LoadDropdown(trangThaiDuyet);

            return View(lst.ToPagedList(page, 10));
        }

        // CHI TIẾT

        public ActionResult ChiTiet(string maToaNha)
        {
            var obj = _bus.LayChiTiet(maToaNha);
            if (obj == null) return HttpNotFound();
            return View(obj);
        }

        // DUYỆT 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Duyet(string maToaNha)
        {
            var admin = GetUserOnline();
            bool kq = _bus.DuyetToaNha(maToaNha, admin.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Duyệt tòa nhà thành công. Chủ trọ đã được thông báo."
                : "Duyệt thất bại, vui lòng thử lại.";

            return RedirectToAction("DanhSach");
        }

        // TỪ CHỐI

        [HttpGet]
        public ActionResult TuChoi(string maToaNha)
        {
            var obj = _bus.LayChiTiet(maToaNha);
            if (obj == null) return HttpNotFound();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoi(string maToaNha, string lyDoTuChoi)
        {
            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
            {
                TempData["Error"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction("TuChoi", new { maToaNha });
            }

            var admin = GetUserOnline();
            bool kq = _bus.TuChoiToaNha(maToaNha, admin.MaNguoiDung, lyDoTuChoi);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã từ chối tòa nhà. Chủ trọ đã được thông báo."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        // TẠM NGƯNG

        [HttpGet]
        public ActionResult TamNgung(string maToaNha)
        {
            var obj = _bus.LayChiTiet(maToaNha);
            if (obj == null) return HttpNotFound();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TamNgung(string maToaNha, string lyDoTuChoi)
        {
            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
            {
                TempData["Error"] = "Vui lòng nhập lý do tạm ngưng.";
                return RedirectToAction("TamNgung", new { maToaNha });
            }

            var admin = GetUserOnline();
            bool kq = _bus.TamNgungToaNha(maToaNha, admin.MaNguoiDung, lyDoTuChoi);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã tạm ngưng tòa nhà. Chủ trọ đã được thông báo."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        // MỞ LẠI

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MoLai(string maToaNha)
        {
            var admin = GetUserOnline();
            bool kq = _bus.MoLaiToaNha(maToaNha, admin.MaNguoiDung);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã mở lại tòa nhà. Chủ trọ đã được thông báo."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        // XÓA

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maToaNha)
        {
            bool kq = _bus.XoaAdmin(maToaNha);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa tòa nhà."
                : "Xóa thất bại.";

            return RedirectToAction("DanhSach");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanXoa(string maToaNha)
        {
            bool kq = _bus.XoaAdmin(maToaNha);

            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa tòa nhà theo yêu cầu."
                : "Xóa thất bại!";

            return RedirectToAction("DanhSach");
        }

        // Admin từ chối yêu cầu xóa → trả về DaDuyet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoiXoa(string maToaNha)
        {
            var db = DataProvider.Entities;
            var obj = db.ToaNhas.FirstOrDefault(x => x.MaToaNha == maToaNha);
            if (obj == null)
            {
                TempData["Error"] = "Không tìm thấy tòa nhà.";
                return RedirectToAction("DanhSach");
            }

            obj.TrangThaiDuyet = "DaDuyet";
            obj.LyDoTuChoi = null;
            obj.NgayCapNhat = DateTime.Now;

            db.Configuration.ValidateOnSaveEnabled = false;
            try { db.SaveChanges(); }
            finally { db.Configuration.ValidateOnSaveEnabled = true; }

            TempData["Success"] = "Đã từ chối yêu cầu xóa. Tòa nhà trở về trạng thái Đã duyệt.";
            return RedirectToAction("DanhSach");
        }
    }
}