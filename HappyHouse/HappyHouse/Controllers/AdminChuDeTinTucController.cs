using HappyHouse.Models;
using PagedList;
using System.Web.Mvc;

namespace HappyHouse.Controllers
{
    public class AdminChuDeTinTucController
        : AdminBaseController
    {
        private readonly ChuDeTinTucBusiness _bus =
            new ChuDeTinTucBusiness();

        private void LoadDropdown(string selected = null)
        {
            var lst = new[]
            {
                new { Value = "",
                      Text  = "--- Tất cả ---" },
                new { Value = "true",
                      Text  = "Hoạt động"      },
                new { Value = "false",
                      Text  = "Đã ẩn"          },
            };
            ViewBag.DsTrangThai = new SelectList(
                lst, "Value", "Text", selected);
        }

        [HttpGet]
        public ActionResult DanhSach(int page = 1,
                                      string tuKhoa = null,
                                      string trangThai = null)
        {
            return HienThiDanhSach(page, tuKhoa, trangThai);
        }

        [HttpPost]
        public ActionResult DanhSach(string tuKhoa,
                                      string trangThai,
                                      int page = 1)
        {
            return HienThiDanhSach(page, tuKhoa, trangThai);
        }

        private ActionResult HienThiDanhSach(
            int page, string tuKhoa, string trangThai)
        {
            bool? filter = null;
            if (trangThai == "true") filter = true;
            if (trangThai == "false") filter = false;

            var lst = _bus.LayDanhSach(tuKhoa, filter);

            ViewBag.TuKhoa = tuKhoa;
            ViewBag.TrangThai = trangThai;
            LoadDropdown(trangThai);

            return View(lst.ToPagedList(page, 10));
        }

        [HttpGet]
        public ActionResult ThemMoi()
        {
            return View(new ChuDeTinTuc());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMoi(ChuDeTinTuc obj)
        {
            if (string.IsNullOrWhiteSpace(obj.TenChuDe))
                ModelState.AddModelError("TenChuDe",
                    "Tên chủ đề không được để trống!");

            if (!ModelState.IsValid)
                return View(obj);

            bool kq = _bus.ThemMoi(obj);
            if (kq)
            {
                TempData["Success"] =
                    "Thêm chủ đề thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("TenChuDe",
                "Tên chủ đề này đã tồn tại!");
            return View(obj);
        }

        [HttpGet]
        public ActionResult SuaThongTin(string maChuDe)
        {
            var obj = _bus.LayChiTiet(maChuDe);
            if (obj == null) return HttpNotFound();
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaThongTin(ChuDeTinTuc obj)
        {
            if (string.IsNullOrWhiteSpace(obj.TenChuDe))
                ModelState.AddModelError("TenChuDe",
                    "Tên chủ đề không được để trống!");

            if (!ModelState.IsValid)
                return View(obj);

            bool kq = _bus.CapNhat(obj);
            if (kq)
            {
                TempData["Success"] =
                    "Cập nhật chủ đề thành công!";
                return RedirectToAction("DanhSach");
            }

            ModelState.AddModelError("TenChuDe",
                "Tên chủ đề này đã tồn tại!");
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiTrangThai(string maChuDe)
        {
            bool kq = _bus.DoiTrangThai(maChuDe);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Cập nhật trạng thái thành công."
                : "Thao tác thất bại.";

            return RedirectToAction("DanhSach");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Xoa(string maChuDe)
        {
            bool kq = _bus.Xoa(maChuDe);
            TempData[kq ? "Success" : "Error"] = kq
                ? "Đã xóa chủ đề thành công."
                : "Không thể xóa! Chủ đề đang có bài viết.";

            return RedirectToAction("DanhSach");
        }
    }
}